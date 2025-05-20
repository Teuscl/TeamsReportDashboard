import { LoginForm } from "@/components/Login/login-form";
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import axiosConfig  from "../../services/axiosConfig"; 

const LoginPage = () => {
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState<boolean>(false);
    const navigate = useNavigate();

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        setError("");

        try {
            const response = await axiosConfig.post("/auth/login", {
                email,
                password,
            });
            const token = response.data.token;
            axiosConfig.defaults.headers.common["Authorization"] = `Bearer ${token}`;
            navigate("/dashboard");
        } catch (err) {
            setError("Email ou senha inválidos.");
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
    )
}
export default LoginPage;