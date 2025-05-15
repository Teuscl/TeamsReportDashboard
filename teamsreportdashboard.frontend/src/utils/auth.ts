// utils/auth.ts
import { jwtDecode } from "jwt-decode";
import JwtUser from "../types/JwtUser";

export function getToken(): string | null {
    return localStorage.getItem("token");
}
export function getCurrentUser(): JwtUser | null {
    const token = getToken();
    if (!token) return null;
    try {
        return jwtDecode<JwtUser>(token);
    } catch {
        return null;
    }
}
export function logout(): void {
    localStorage.removeItem("token");
    window.location.href = "/";
}
