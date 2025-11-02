'use strict';

const API_BASE = "https://localhost:7088/api/admin";

async function apiRequest(endpoint, method = "GET", data = null) {
    const options = {
        method,
        headers: { "Content-Type": "application/json" },
    };
    if (data) options.body = JSON.stringify(data);

    const res = await fetch(`${API_BASE}${endpoint}`, options);
    if (!res.ok) {
        const text = await res.text();
        throw new Error(`API error (${res.status}): ${text}`);
    }
    return res.status !== 204 ? await res.json() : null;
}
