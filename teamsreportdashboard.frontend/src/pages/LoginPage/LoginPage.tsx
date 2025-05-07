// src/components/LoginPage.tsx

import React, { useState } from "react";
import "./LoginPage.css";
import axios from "../../services/axiosConfig";
import { useNavigate } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import {
    Alert,
    Button,
    Col,
    Container,
    Form,
    FormControl,
    FormGroup,
    FormLabel,
    Row,
    InputGroup,
} from "react-bootstrap";
import { FaEnvelope, FaLock } from "react-icons/fa";

const LoginPage = ({ onLogin }: { onLogin: any }) => {
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    const [error, setError] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(false);
    const navigate = useNavigate();

    const getCurrentUser = () => {
        const token = localStorage.getItem("token");
        if (!token) return null;

        try {
            return jwtDecode(token);
        } catch {
            return null;
        }
    };

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        setError("");

        try {
            const response = await axios.post("/auth/login", {
                email,
                password,
            });

            localStorage.setItem("token", response.data.token);
            localStorage.setItem("refreshToken", response.data.refreshToken);
            onLogin(getCurrentUser());
            navigate("/dashboard");
        } catch (err) {
            setError("E-mail ou senha incorretos. Tente novamente.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <Container className="d-flex justify-content-center align-items-center vh-100">
            <Row className="w-75">
                <Col md={6} className="mx-auto">
                    <div className="border p-4 shadow-lg bg-body-tertiary rounded login-box">
                        <h3 className="text-center mb-4">Login</h3>
                        <Form onSubmit={handleLogin}>
                            <FormGroup controlId="email" className="mb-3">
                                <FormLabel>E-mail</FormLabel>
                                <InputGroup>
                                    <InputGroup.Text>
                                        <FaEnvelope />
                                    </InputGroup.Text>
                                    <FormControl
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        placeholder="Digite seu e-mail"
                                        required
                                    />
                                </InputGroup>
                            </FormGroup>

                            <FormGroup controlId="password" className="mb-3">
                                <FormLabel>Senha</FormLabel>
                                <InputGroup>
                                    <InputGroup.Text>
                                        <FaLock />
                                    </InputGroup.Text>
                                    <FormControl
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        placeholder="Digite sua senha"
                                        required
                                    />
                                </InputGroup>
                            </FormGroup>

                            <div className="text-center mt-4">
                                <Button
                                    variant="success"
                                    type="submit"
                                    disabled={loading}
                                    className="w-100"
                                >
                                    {loading ? "Carregando..." : "Entrar"}
                                </Button>
                            </div>
                            <div className="text-center mt-3">
                                <a href="/forgot-password">Esqueci minha senha</a>
                            </div>

                            {error && (
                                <Alert variant="danger" className="mt-3 text-center">
                                    {error}
                                </Alert>
                            )}
                        </Form>
                    </div>
                </Col>
            </Row>
        </Container>
    );
};

export default LoginPage;
