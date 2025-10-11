import api from './api';
import { Collection } from './types';

export const randomApi = {
  // Get random collection
  getRandomCollection: async (): Promise<Collection> => {
    const response = await api.get('/random');
    return response.data;
  },
};

