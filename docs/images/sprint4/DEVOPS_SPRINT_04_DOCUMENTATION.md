# EcoTrack – Sprint 04 DevOps Report

| Item | Details |
|---|---|
| Project | EcoTrack – E-Waste Tracking and Recycling Marketplace |
| Module | SE3022 – Case Study Project (Group 34) |
| Sprint | Sprint 04 |
| Role | DevOps Engineer |
| Name | Diloosha Rajapaksha (IT24610798) |
| Date | October 2026 |
| Jira tasks | ECO-171, ECO-172, ECO-173, ECO-88, ECO-174 |
| Live application | http://ecotrack.eastasia.cloudapp.azure.com:5173 |
| Previous DevOps report | `docs/DEVOPS_SPRINT_03_DOCUMENTATION.md` (Akini Panapitiya) |

---

## 1. Summary

In Sprint 04 my goal as DevOps was to take EcoTrack from a project that ran on our laptops (and a test server on AWS) to a real production deployment on **Microsoft Azure**, where every release is tested and deployed automatically.

By the end of the sprint:

- The CI pipeline checks **all four microservices and the React frontend** on every push and pull request.
- Kafka has all the **topics and dead-letter topics** needed for the Sprint 04 stories, and I verified that events flow end to end.
- The whole system runs on an **Azure virtual machine**, with every container reporting healthy.
- Merging into the `Deploy` branch **deploys to Azure automatically**, checks the health of every service, and **rolls back by itself** if anything fails.

| # | Deliverable | Jira | Result |
|---|---|---|---|
| 1 | Extend CI to every service and the frontend, with real quality gates | ECO-172 | Done – 12 automated checks per push/PR |
| 2 | Kafka topics, dead-letter topics, retention and end-to-end test | ECO-171 | Done – 16 topics created automatically |
| 3 | Azure production environment | ECO-88 / ECO-173 | Done – all containers healthy on Azure |
| 4 | CD pipeline with health checks and automatic rollback | ECO-173 | Done – merge to `Deploy` deploys to Azure |
| 5 | DevOps documentation | ECO-174 | Done – this report |

---

## 2. Changes Since Sprint 03 and Why I Made Them

The assignment asks for documented justifications for any changes made to the CI/CD pipeline in later sprints. This table lists every change I made in Sprint 04.

| # | Area | Before (Sprint 03) | After (Sprint 04) | Reason |
|---|---|---|---|---|
| 1 | Deployment target | AWS EC2 | Azure virtual machine | The assignment specifies Azure. Using the same Docker Compose setup keeps production identical to local development. |
| 2 | Health check after deploy | Ended with `\|\| echo`, so it could never fail | Real checks on all 4 APIs and the website, with retries | A broken deployment must fail visibly, not show green. |
| 3 | Rollback | None | Automatic rollback to the previous version | If a release breaks, users keep the last working version. |
| 4 | Deploy trigger | Two branches (`Deploy` and `deploy`) | One `Deploy` branch plus a manual "Run workflow" button, one deploy at a time | One clear release branch and no overlapping deployments. |
| 5 | Docker image builds in CI | Only on pull requests, partial build, 4 services | Every push and PR, full image, 4 services plus the web app | Proves the exact images we run in production can be built. |
| 6 | Coverage quality gate | Only checked that a number existed | Fails if line coverage drops below 20% | A gate that can never fail is not a gate. 20% is just below our current level, so coverage cannot go backwards. |
| 7 | Extra CI checks | None | Frontend build, Docker Compose validation, dependency vulnerability audit | Catches UI errors, deployment-config errors and vulnerable packages early. |
| 8 | Kafka topics | 3 topics, each dead-letter topic written by hand | 8 topics, dead-letter topic created automatically, retention set | Covers Sprint 04 events and makes it impossible to forget a dead-letter topic. |
| 9 | Kafka storage | Volume attached to a folder Kafka did not use | Kafka now stores data in the volume | Topics and events were being lost whenever the container restarted. |
| 10 | Kafka access for developers | Reachable only from inside Docker | Extra listener on `localhost:29092` | Developers running services with `dotnet run` could not connect. |
| 11 | Database setup in Docker | `scripts/init-databases.sql` | The migration scripts in `database/migrations/` | The old script was out of date and did not match the code. |
| 12 | Marketplace database setting | `ConnectionStrings__DefaultConnection` | `ConnectionStrings__MarketplaceDb` | The code reads `MarketplaceDb`, so Marketplace could not find its database in Docker. |
| 13 | API Docker images | No `curl` inside | `curl` installed | Health checks showed "unhealthy" even though the services worked. |
| 14 | Shell script line endings | Depended on the developer's OS | Forced to Linux format with `.gitattributes` | Windows line endings broke the Kafka script inside Linux containers. |

---

## 3. System Overview

### 3.1 Services

| Service | Port | Database | Health check | What it does |
|---|---|---|---|---|
| Identity | 5001 | ecotrack_identity_db | /identity/health | Login (JWT), profiles, KYC, roles, audit log |
| Logistics | 5002 | ecotrack_logistics_db | /logistics/health | Pickup requests, scheduling, collection, disposal certificates |
| Marketplace | 5003 | ecotrack_marketplace_db | /marketplace/health | Item valuations, refurbished listings, orders |
| Analytics | 5004 | ecotrack_analytics_db | /analytics/health | Sustainability metrics and reports |
| Web app | 80 and 5173 | – | / | React website served by Nginx |
| Kafka 3.8 | 9092 (internal), 29092 (developers) | kafka-data volume | Topic list command | Sends events between services |
| MySQL 8.0 | 3306 (private) | db-data volume | mysqladmin ping | One separate database per service |
| Prometheus | 9090 | prometheus-data volume | /-/healthy | Collects metrics |
| Grafana | 3000 | grafana-data volume | /api/health | Monitoring dashboards |

Each microservice has its own Dockerfile, Docker image, container and database. Any one of them can be rebuilt or restarted while the others keep running, which meets the assignment's requirement for independently deployable services.

### 3.2 How the deployment is laid out

```
                      Internet (users)
                            |
                 Azure Network Security Group
        (open: 22, 80, 443, 5001-5004, 5173, 3000, 9090)
                            |
   +--------------------------------------------------------+
   |  Azure VM: ecotrack-vm (Ubuntu 24.04, 2 vCPU, 8 GB)     |
   |                                                        |
   |   web  ->  identity | logistics | marketplace | analytics
   |                 \        |          /          /       |
   |                  +---- MySQL (4 databases) ---+        |
   |   logistics --events--> Kafka (16 topics)              |
   |   Prometheus --scrapes--> APIs     Grafana --> Prometheus
   +--------------------------------------------------------+
```

MySQL and Kafka have **no open port to the internet**. They can only be reached from inside the VM. When I need to look at the database, I connect through an SSH tunnel in MySQL Workbench.

### 3.3 Azure resources

| Resource | Name | Details |
|---|---|---|
| Resource group | ecotrack-rg | Holds everything for the project |
| Virtual machine | ecotrack-vm | Ubuntu 24.04, Standard_D2as_v4 (2 vCPU, 8 GB RAM), East Asia |
| OS disk | ecotrack-vm_OsDisk | 64 GB |
| Public IP | ecotrack-vm-ip | Static IP with DNS name `ecotrack.eastasia.cloudapp.azure.com` |
| Network security group | ecotrack-vm-nsg | Firewall rules listed in section 3.2 |
| Virtual network | vnet-eastasia-1 | Private network for the VM |

### 3.4 Why I chose an Azure VM with Docker Compose

| Option | Chosen? | Reason |
|---|---|---|
| Azure VM + Docker Compose | Yes | Uses the exact same `docker-compose.yml` we use locally. Runs Kafka, MySQL and monitoring without redesigning anything. Fits the Azure for Students budget. The assignment allows "Azure App Service, or Docker". |
| Azure App Service | No | Would need a separate managed database and a separate Kafka service. Running many containers together is limited. |
| Azure Container Apps or AKS | Future work | Each service could scale and restart on its own, but this needs a container registry, a managed database and managed messaging, which was more time and cost than this sprint allowed. |

**Known limitation:** everything runs on one VM, so if the VM goes down the whole system goes down. For a real production system I would move each service to Azure Container Apps or AKS and use Azure Database for MySQL, keeping the same Docker images.

### 3.5 How I chose the region and VM size

The student subscription only allows certain regions and has small CPU limits per VM family. My first attempts failed, so I used Azure Cloud Shell to check what was actually allowed before creating the VM.

| Step | Command used | What I found |
|---|---|---|
| 1 | `az policy assignment list` | Allowed regions: Malaysia West, Austria East, Indonesia Central, Central India, East Asia |
| 2 | `az vm list-skus` | 8 GB sizes were only available to my subscription in East Asia |
| 3 | `az vm list-usage --location eastasia` | The DASv5 family had a limit of 0 CPUs (the first VM failed with `QuotaExceeded`). The DASv4 family had a limit of 4 CPUs. |
| 4 | Decision | East Asia with Standard_D2as_v4 (2 vCPU, 8 GB). 8 GB is needed for Kafka, MySQL, four .NET APIs and building the images. |

---

## 4. Continuous Integration (ECO-172)

**File:** `.github/workflows/ci.yml`
**Runs on:** every push and pull request to `Dev`, `Deploy` and `main`

The pipeline runs in two stages. Stage 1 checks the code. Stage 2 builds the Docker images, but only if the tests and the frontend build passed.

| Stage | Check | What it does | Why it matters |
|---|---|---|---|
| 1 | Build, Test and Coverage Gate | Builds all services and runs every unit test with code coverage | Any compile error or failing test blocks the merge |
| 1 | Coverage gate (new) | Fails if line coverage is below 20% | Stops coverage from going down |
| 1 | Frontend Build (new) | `npm ci` and `npm run build` for the React app | The website was never checked in CI before |
| 1 | Docker Compose Validation (new) | `docker compose config --quiet` | Catches mistakes in the deployment file before they reach the server |
| 1 | Dependency Audit (new) | Lists vulnerable NuGet and npm packages and saves a report | Shows known security problems. It warns instead of failing, because fixes for third-party packages are not always available. QA reviews the report (ECO-169). |
| 1 | CodeQL (C# and JavaScript) | Scans the code for security problems | Finds issues such as injection risks automatically |
| 2 | Docker Build x5 (new) | Builds the full images for identity, logistics, marketplace, analytics and web, tagged with the commit ID | Proves the production images build. Only runs after stage 1 passes. |

**Result:** 12 checks run on every pull request (screenshot in section 8).

---

## 5. Kafka Event Streaming (ECO-171)

### 5.1 Topics

Topic names follow the pattern `area.thing.event`. Every topic automatically gets a matching dead-letter topic (`<topic>.dlq`) where messages that cannot be processed are sent, so they are not lost and do not block other messages.

| Topic | Sent by | Used for | Message key | Status |
|---|---|---|---|---|
| logistics.pickup.scheduled | Logistics | Notify the user that a pickup is scheduled (ECO-35) | Pickup ID | Sending events |
| logistics.pickup.collected | Logistics | Drop-off record and audit log (ECO-26, ECO-36) | Pickup ID | Sending events |
| ewaste.disposal.certified | Logistics | Disposal certificate issued (ECO-36) | Pickup item ID | Sending events |
| ewaste.item.disposed | Logistics | Environmental audit log (ECO-36) | Item ID | Topic ready |
| marketplace.order.placed | Marketplace | Notify the recycler that an item was ordered (ECO-35) | Order ID | Topic ready |
| marketplace.payment.status.changed | Marketplace | Payment Pending / Paid / Refunded (ECO-31) | Payment ID | Topic ready |
| ewaste.item.refurbished | Marketplace | Environmental audit log (ECO-36) | Item ID | Topic ready |
| recycler.verification.changed | Identity | Recycler KYC approved or rejected | Recycler ID | Topic ready |

### 5.2 Settings

| Setting | Value | Reason |
|---|---|---|
| Partitions per topic | 3 | Up to 3 consumers in one group can share the work |
| Replication factor | 1 | We have one Kafka broker. Production would use 3 brokers and 3 copies. |
| Retention for normal topics | 7 days | Enough time to replay events if a consumer was down |
| Retention for dead-letter topics | 14 days | More time to investigate failed messages |
| Topic creation | `--if-not-exists` | The script is safe to run on every deployment |
| Listener for containers | kafka:9092 | Used by services running inside Docker |
| Listener for developers | localhost:29092 | Used by services started with `dotnet run` |
| Data folder | /var/lib/kafka/data (the Docker volume) | Topics and events survive restarts |

### 5.3 How Logistics sends events

| Design choice | Reason |
|---|---|
| One shared Kafka producer for the whole service | Creating a producer is expensive. One shared instance is faster. |
| `acks = all` and idempotence turned on | A message is only confirmed once Kafka has safely stored it, and retries never create duplicates |
| 5 second timeout, errors are logged instead of crashing | If Kafka is down, the user's action still succeeds because the data is already saved in MySQL |
| Events are sent only after the database update succeeds | Events only describe things that really happened |
| An interface (`IEventPublisher`) is used, so tests can use a fake | Unit tests run without a real Kafka. A new test checks the event is sent. All 33 Logistics tests pass. |

A future improvement is the Transactional Outbox pattern, which guarantees an event is sent even if Kafka is down at that moment.

### 5.4 How I tested it

| Test | Result |
|---|---|
| List all topics | 16 topics (8 topics and 8 dead-letter topics) |
| List topics through the developer listener (29092) | Same 16 topics |
| Restart the Kafka container | Topics still there (storage fix works) |
| Confirm a pickup schedule in the web app | Logistics logged `Published event to logistics.pickup.scheduled (partition 1, offset 0)` |
| Watch the topic with a console consumer | Received the `PickupScheduled` event with the real pickup, user and recycler IDs |

---

## 6. Continuous Deployment to Azure (ECO-88, ECO-173)

### 6.1 How code reaches production

| Step | Branch | What happens |
|---|---|---|
| 1 | feature/ECO-xxx | Work is done on its own branch |
| 2 | Pull request into Dev | All 12 CI checks must pass before merging |
| 3 | Release pull request from Dev into Deploy | CI runs again on the combined code |
| 4 | Merge into Deploy | GitHub Actions deploys to Azure automatically |

### 6.2 What the deployment pipeline does

**File:** `.github/workflows/deploy.yml`

| Step | What happens |
|---|---|
| 1. Trigger | Starts when code is merged into `Deploy`, or when someone presses "Run workflow" |
| 2. One at a time | If two releases happen close together, the second waits for the first |
| 3. Test again | Builds the code and runs every unit test. If anything fails, nothing is deployed. |
| 4. Connect to Azure | Logs in to the VM over SSH with a dedicated deploy key stored as a GitHub secret |
| 5. Save the current version | Records the version that is currently running, to use as a restore point |
| 6. Update | Pulls the new `Deploy` code and runs `docker compose up -d --build` (only changed services are rebuilt) |
| 7. Health check | Checks all 4 API health endpoints and the website, retrying for up to about a minute |
| 8. Automatic rollback | If any check fails, switches back to the saved version, rebuilds it, and marks the run as failed |
| 9. Check from outside | GitHub calls the 4 health endpoints over the internet, the same way a real user would |
| 10. Summary | Writes the deployed commit and all service links on the run's summary page |

### 6.3 Secrets

| Secret | What it is | How it is protected |
|---|---|---|
| AZURE_VM_HOST | The VM's public IP | Hidden as `***` in all logs |
| AZURE_VM_SSH_KEY | Private key used only by GitHub Actions | Separate from my personal key so it can be removed on its own. Not stored on the VM. Rotated after it was accidentally shown on screen during setup. |

### 6.4 Problems I found while deploying

These bugs did not show up on our laptops. They only appeared on a clean server.

| Problem | What happened | How I fixed it |
|---|---|---|
| Marketplace database setting had the wrong name | Marketplace could not connect to its database in Docker | Renamed the setting to `MarketplaceDb` in `docker-compose.yml` |
| Database script was out of date | Tables did not match the code (for example `Valuations` instead of `ItemValuations`) | Docker now uses the same migration scripts as developers |
| No `curl` inside the API images | All four APIs showed "unhealthy" even though they worked | Installed `curl` in each image |
| Different MySQL password on my laptop | Login returned error 500 when running locally | Set the password through an environment variable, never committed to Git |

### 6.5 Rollback plan

| Situation | What to do |
|---|---|
| A health check fails during a deployment | Nothing. The pipeline restores the previous version automatically. |
| A bug is found after a successful deployment | Use GitHub's "Revert" button on the release pull request. The revert merges into `Deploy` and the pipeline redeploys the previous code. |
| GitHub Actions is unavailable | Connect over SSH and run `git checkout <last good commit>` then `docker compose up -d --build` |
| Only one service is misbehaving | `docker compose restart <service>`. The other services keep running. |

### 6.6 Smoke test after deployment

| Check | Result |
|---|---|
| `docker compose ps` | All 4 APIs, MySQL and Kafka healthy. kafka-init finished successfully. |
| 4 health endpoints, from inside the VM and from the internet | All return `"status":"healthy"` |
| Website | Opens at the Azure address |
| Register and log in | Works (Identity, MySQL and the website working together) |
| Grafana | Login page opens on port 3000 |
| Admin account | Created by registering normally and then changing the role in the database. Admins cannot register themselves. |
| Automatic deployment | Merge into `Deploy` → tests passed → deployed → health checks passed |

---

## 7. Security

| Area | What is in place | Planned improvement |
|---|---|---|
| Containers | Run as a non-root user. Runtime images do not include build tools. | – |
| Network | Only the web, API and monitoring ports are open. MySQL and Kafka are private. | Bind MySQL and Kafka to localhost on the VM as an extra layer |
| Server login | SSH key only, no passwords | Limit SSH to known IP addresses, or use Azure Bastion |
| Secrets | Stored as encrypted GitHub secrets and hidden in logs. Dedicated deploy key, rotated after exposure. | Move database passwords from `docker-compose.yml` into an `.env` file or Azure Key Vault |
| Code scanning | CodeQL and dependency audit on every change | – |
| Admin access | Admin role can only be given in the database | – |
| HTTPS | Not yet. The site uses plain HTTP. | Add a TLS certificate (for example Nginx with Let's Encrypt) and redirect HTTP to HTTPS |

---

## 8. Evidence

Screenshots are saved in `docs/images/sprint4/`.

| # | What the screenshot shows | File name |
|---|---|---|
| 1 | All CI checks green on a pull request | ci-checks.png |
| 2 | Coverage gate passed on the run summary | ci-coverage-gate.png |
| 3 | The 16 Kafka topics | kafka-topics.png |
| 4 | Logistics sending an event and the consumer receiving it | kafka-end-to-end.png |
| 5 | Resources in the `ecotrack-rg` resource group | azure-resources.png |
| 6 | Cloud Shell region, size and quota checks | azure-quota.png |
| 7 | Firewall (NSG) inbound rules | azure-nsg.png |
| 8 | `docker compose ps` on the VM with all services healthy | vm-compose-ps.png |
| 9 | Successful "Deploy to Azure VM" run and its summary | cd-run.png |
| 10 | Logged-in dashboard on the Azure site | app-dashboard.png |

---

## 9. Jira Traceability

| Jira | Task | Branch | Status |
|---|---|---|---|
| ECO-172 | Extend CI for all services and add quality gates | feature/ECO-172-sprint4-ci-pipeline | Done |
| ECO-171 | Kafka topics, dead-letter topics, retention and end-to-end test | feature/ECO-171-sprint4-kafka-topics, feature/ECO-171-kafka-logistics-producer | Done |
| ECO-88 | Azure deployment pipeline (CD) | feature/ECO-173-azure-deployment | Done |
| ECO-173 | Production deployment on Azure, smoke test and rollback plan | feature/ECO-173-azure-deployment | Done |
| ECO-174 | DevOps documentation | feature/ECO-174-devops-sprint4-docs | Done |

---

## 10. How to Run and Operate the System

### 10.1 Run everything on a new machine

| Step | Command |
|---|---|
| 1. Get the code | `git clone https://github.com/AkiniPanapitiya/ecotrack.git` |
| 2. Go into the folder | `cd ecotrack` |
| 3. Build and start all services | `docker compose up -d --build` |
| 4. Check everything is healthy | `docker compose ps` |
| 5. Open the website | http://localhost:5173 |

The databases and Kafka topics are created automatically on the first start.

### 10.2 Everyday commands on the Azure VM

| Task | Command |
|---|---|
| Connect to the server | `ssh -i <key-file>.pem azureuser@<server-ip>` |
| See the status of all services | `docker compose ps` |
| See a service's logs | `docker logs ecotrack-logistics --tail 50` |
| Stop one service | `docker compose stop logistics` |
| Start one service | `docker compose start logistics` |
| List Kafka topics | `docker exec ecotrack-kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --list` |
| Open the MySQL shell | `docker exec -it ecotrack-db mysql -uroot -p` |
| Run a command without SSH | Azure portal → ecotrack-vm → Run command → RunShellScript |
| Save credit when not needed | Azure portal → ecotrack-vm → Stop (start it again before demos) |

---

## 11. Sprint 04 DevOps Retrospective

| What went well | What was difficult | Action for next time | Owner |
|---|---|---|---|
| Code now moves from feature branch to Dev to Deploy with CI checks at every step | The student subscription blocked the first VM sizes and regions | Check region, size and quota with Azure CLI before creating resources (now in this report) | DevOps |
| Automatic deployment with health checks and rollback works end to end | Some bugs only appeared on a clean server | Deploy to a clean environment early in every sprint | DevOps |
| Kafka was tested end to end with a real event | The Kafka consumer stories (ECO-35, ECO-36) were not started in time | Agree event formats during sprint planning and track cross-role dependencies on the Jira board | Whole team |
| CI now covers the website, the deployment file and dependencies | A deploy key was briefly shown on screen during setup | Clear the terminal after handling secrets. The key was rotated. | DevOps |

---

## 12. Limitations and Next Steps

| Area | Current state | Next step |
|---|---|---|
| Kafka consumers | Topics and the sending side are ready | Developer builds the notification (ECO-35) and audit log (ECO-36) consumers |
| Other event senders | Logistics sends events | Marketplace and Identity use the same `IEventPublisher` approach |
| Hosting | One VM | Move to Azure Container Apps or AKS so each service scales on its own |
| Deployment speed | Rebuilds any changed service | Only redeploy services whose folders changed |
| HTTPS | Plain HTTP | Add a TLS certificate and redirect to HTTPS |

---

## 13. AI Assistance Disclosure

| Item | Details |
|---|---|
| Tool used | Claude (Anthropic) |
| Used for | Planning the sprint tasks, explaining concepts, reviewing configuration files and troubleshooting errors |
| My responsibility | I applied, tested and verified every change myself, and I can explain each part of this report |
