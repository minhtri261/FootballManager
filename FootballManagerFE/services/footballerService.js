import { apiRequest } from "../api.js";

export const footballerService = {
  getAll: () => apiRequest("/footballers"),
  get: (id) => apiRequest(`/footballers/${id}`),
  getByClub: (clubId) => apiRequest(`/footballers/by-club/${clubId}`),
  create: (data) => apiRequest("/footballers", "POST", data),
  update: (id, data) => apiRequest(`/footballers/${id}`, "PUT", data),
  remove: (id) => apiRequest(`/footballers/${id}`, "DELETE"),
};
