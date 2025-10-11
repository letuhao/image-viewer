import axios from 'axios';

// API Base URL - use relative path for Vite proxy
const API_BASE_URL = '/api/v1';

// Create axios instance with default config
const apiInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for adding auth tokens
apiInstance.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token');
    
    // Debug logging
    console.log('[API Interceptor] Request URL:', config.url);
    console.log('[API Interceptor] Token from localStorage:', token ? `${token.substring(0, 20)}...` : 'NO TOKEN');
    
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      console.log('[API Interceptor] Authorization header added');
    } else {
      console.warn('[API Interceptor] No token found in localStorage!');
    }
    
    // Add debug header to verify interceptor is running
    config.headers['X-Debug-Interceptor'] = 'active';
    
    // Log all headers being sent
    console.log('[API Interceptor] All headers:', config.headers);
    
    return config;
  },
  (error) => {
    console.error('[API Interceptor] Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle common errors
    if (error.response) {
      // Server responded with error status
      const status = error.response.status;
      const message = error.response.data?.message || error.message;

      switch (status) {
        case 401:
          // Unauthorized - redirect to login (future)
          console.error('Unauthorized access');
          break;
        case 403:
          console.error('Forbidden:', message);
          break;
        case 404:
          console.error('Resource not found:', message);
          break;
        case 500:
          console.error('Server error:', message);
          break;
        default:
          console.error('API error:', message);
      }
    } else if (error.request) {
      // Request made but no response
      console.error('Network error: No response from server');
    } else {
      // Error in request configuration
      console.error('Request error:', error.message);
    }

    return Promise.reject(error);
  }
);

// Export the configured instance (with interceptors attached)
// IMPORTANT: Export AFTER interceptors are registered to ensure they're included
export const api = apiInstance;
export default apiInstance;

