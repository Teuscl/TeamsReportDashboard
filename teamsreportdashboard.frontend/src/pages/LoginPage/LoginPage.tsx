import { LoginForm } from "@/components/Login/login-form";
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import axios from "axios";

const LoginPage = () => {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const { login } = useAuth();

    const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            const success = await login({ email, password });
            if (success) {
                navigate("/dashboard");
            } else {
                setError("Email ou senha inválidos.");
            }
        } catch (err) {
            if (axios.isAxiosError(err) && err.response?.status === 401) {
                setError("Email ou senha inválidos.");
            } else {
                setError("Erro inesperado. Tente novamente mais tarde.");
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex min-h-svh w-full items-center justify-center p-6 md:p-10 bg-background">
            <div className="w-full max-w-sm">
                <LoginForm
                    email={email}
                    password={password}
                    loading={loading}
                    error={error}
                    onEmailChange={(e) => setEmail(e.target.value)}
                    onPasswordChange={(e) => setPassword(e.target.value)}
                    onSubmit={handleLogin}
                />
            </div>
        </div>
    );
};

export default LoginPage;
