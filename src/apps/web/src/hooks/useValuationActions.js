import { useState, useCallback } from 'react';
import { valuationApi } from '../services/valuationApi';

export function useValuationActions() {
  const createValuation = useCallback(async (pickupItemId, data) => {
    const res = await valuationApi.createValuation(pickupItemId, data);
    return { success: true, data: res.data };
  }, []);

  const updateValuation = useCallback(async (pickupItemId, data) => {
    const res = await valuationApi.updateValuation(pickupItemId, data);
    return { success: true, data: res.data };
  }, []);

  const getValuation = useCallback(async (pickupItemId) => {
    const res = await valuationApi.getValuation(pickupItemId);
    return res.data;
  }, []);

  return { createValuation, updateValuation, getValuation };
}
