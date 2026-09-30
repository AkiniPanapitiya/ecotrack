import { useState, useEffect } from 'react';
import { ListingService } from './services/listingApi';

export function useListings(keyword = '', page = 1, pageSize = 12) {
  const [listings, setListings] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageCount, setPageCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadListings();
  }, [keyword, page, pageSize]);

  const loadListings = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await ListingService.browse(keyword, page, pageSize);
      setListings(data.listings || []);
      setTotalCount(data.pagination?.totalCount || 0);
      setPageCount(data.pagination?.pageCount || 0);
    } catch (err) {
      setError(err.message || 'Failed to load listings');
    } finally {
      setLoading(false);
    }
  };

  return { listings, totalCount, pageCount, loading, error, refresh: loadListings };
}
