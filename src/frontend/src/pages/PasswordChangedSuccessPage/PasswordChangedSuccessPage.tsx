// src/pages/AuthFeedback/PasswordChangedSuccessPage.tsx
import React, { useEffect, useRef } from 'react'; // Adicionado useRef
import { useNavigate, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle2 } from 'lucide-react';
import { useAuth } from '@/context/AuthContext'; // 👈 Importe useAuth
import { toast } from 'sonner';

const PasswordChangedSuccessPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout, isAuthenticated } = useAuth(); // 👈 Obtenha logout e isAuthenticated
  const logoutCalledRef = useRef(false); // 👈 Ref para garantir que logout seja chamado apenas uma vez

  useEffect(() => {
    // Verifica se viemos da página de alteração de senha E se o usuário ainda está "logado" no contexto
    if (location.state?.passwordJustChanged && isAuthenticated && !logoutCalledRef.current) {
      logoutCalledRef.current = true; // Marca que vamos chamar logout
      const performLogout = async () => {
        toast.info("Sessão finalizada. Redirecionando para login..."); // Toast informativo
        await logout(); // Efetivamente faz o logout (backend e frontend context)
        // O redirecionamento para '/' acontecerá pelo timer abaixo ou pelo botão
      };
      performLogout();
    }
    // Limpa o estado da navegação para não disparar o logout novamente se a página for revisitada de alguma forma
    // (embora o timer de redirect torne isso menos provável)
    if (location.state?.passwordJustChanged) {
        navigate(location.pathname, { replace: true, state: {} });
    }

  }, [location.state, isAuthenticated, logout, navigate]);

  useEffect(() => {
    // Timer para redirecionar para a página de login
    const timer = setTimeout(() => {
      navigate('/'); 
    }, 5000); 

    return () => clearTimeout(timer);
  }, [navigate]);

  return (
    <div className="flex min-h-svh w-full items-center justify-center bg-background p-4 md:p-6">
      <Card className="w-full max-w-md text-center">
        <CardHeader>
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-100 mb-5">
            <CheckCircle2 className="h-10 w-10 text-green-600" />
          </div>
          <CardTitle className="text-2xl font-bold">Senha Alterada com Sucesso!</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-muted-foreground">
            Sua senha foi redefinida. Você foi desconectado e será redirecionado para a página de login em alguns instantes.
          </p>
          <Button className="w-full mt-2" onClick={() => navigate('/')}>
            Ir para Login Agora
          </Button>
        </CardContent>
      </Card>
    </div>
  );
};

export default PasswordChangedSuccessPage;