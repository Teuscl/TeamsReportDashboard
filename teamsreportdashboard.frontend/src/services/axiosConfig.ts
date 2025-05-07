// src/services/axiosConfig.ts

import axios from 'axios';

const axiosConfig = axios.create({
    baseURL: 'https://localhost:44330/api', // Substitua pela URL do seu backend
    headers: {
        'Content-Type': 'application/json',
    },
});

export default axiosConfig;
