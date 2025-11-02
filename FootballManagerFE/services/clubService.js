import { apiRequest } from "../api.js";

export const clubService = {
    getAll: () => apiRequest("/clubs"),
    get: (id) => apiRequest(`/clubs/${id}`),
    getWithPlayers: (id) => apiRequest(`/clubs/${id}/players`),
    create: (data) => apiRequest("/clubs", "POST", data),
    update: (id, data) => apiRequest(`/clubs/${id}`, "PUT", data),
    remove: (id) => apiRequest(`/clubs/${id}`, "DELETE"),
};
