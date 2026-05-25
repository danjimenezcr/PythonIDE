// config.js — shared configuration for all pages
// Since the frontend and backend are served from the same Apache server,
// a relative path is used. Change this if the backend moves to a different host.
const API = window.location.hostname === 'localhost'
    ? 'http://192.9.149.63/backend/api'
    : '/backend/api';