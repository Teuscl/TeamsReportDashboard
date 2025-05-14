// src/services/axiosConfig.ts

import axios from 'axios';

const axiosConfig = axios.create({
    baseURL: 'https:/localhost:7258/', // Substitua pela URL do seu backend
    headers: {
        'Content-Type': 'application/json',
    },
});

export default axiosConfig;
