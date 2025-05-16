import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"


interface LoginFormProps extends React.ComponentProps<"div"> {
  email: string;
  password: string;
  loading: boolean;
  error: string | null;
  onEmailChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onPasswordChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onSubmit: (e: React.FormEvent) => void;
}

export function LoginForm({
  className,
  email,
  password,
  loading,
  error,
  onEmailChange,
  onPasswordChange,
  onSubmit,
  ...props
}: LoginFormProps) {
  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card>
        <CardHeader>
          <CardTitle>Login</CardTitle>
          <CardDescription>
            Entre com seu email e senha para acessar sua conta.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit}>
            <div className="flex flex-col gap-6">
              <div className="grid gap-3">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="m@example.com"
                  required
                  value={email}
                  onChange={onEmailChange}
                />
              </div>
              <div className="grid gap-3">
                <div className="flex items-center">
                  <Label htmlFor="password">Senha</Label>
                  <a
                    href="#"
                    className="ml-auto inline-block text-sm underline-offset-4 hover:underline"
                  >
                    Esqueceu sua senha?
                  </a>
                </div>
                <Input 
                id="password"
                type="password"
                required
                value={password}
                onChange={onPasswordChange}
                />
              </div>
              {error && (
                <p className="text-sm text-red-500 text-center">{error}</p>
              )}
              <div className="flex flex-col gap-3">
                <Button type="submit" className="w-full bg-white text-black" disabled={loading}>
                  {loading ? "Carregando..." : "Login"}                  
                </Button>                
              </div>
            </div>
            <div className="mt-4 text-center text-sm">
              Não tem uma conta?{" "}
              <a href="#" className="underline underline-offset-4">
                Registre-se
              </a>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
