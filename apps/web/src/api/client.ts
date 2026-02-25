// H-2: No localStorage — auth is handled via HttpOnly ca_jwt cookie set by the API.
// The browser sends the cookie automatically on every request (withCredentials).
import axios from "axios";

export const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

const client = axios.create({
  baseURL: API_BASE,
  withCredentials: true,   // sends HttpOnly ca_jwt cookie on every request
});

client.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      window.location.href = "/login";
    }
    return Promise.reject(err);
  }
);

export default client;