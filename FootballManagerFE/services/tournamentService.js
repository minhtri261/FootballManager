import { apiRequest } from "../api.js";

export const tournamentService = {
  getAll: () => apiRequest("/tournaments"),
  get: (id) => apiRequest(`/tournaments/${id}`),
  getWithClubs: (id) => apiRequest(`/tournaments/${id}/clubs`),
  create: (data) => apiRequest("/tournaments", "POST", data),
  update: (id, data) => apiRequest(`/tournaments/${id}`, "PUT", data),
  remove: (id) => apiRequest(`/tournaments/${id}`, "DELETE"),
};
