// src/pages/Users/UsersPage.tsx
import React, { useEffect, useState, useCallback } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '../../components/CustomTable/DataTable';
import { MoreHorizontal, ArrowUpDown } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { UserFormModal } from '@/components/UserFormModal';
import { getUsers, deleteUser } from '@/services/userService';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { User } from '@/types/User';
import { RoleEnum, getRoleLabel } from '@/utils/role';
import AdminResetPasswordModal from '@/components/AdminResetPasswordModal';

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const { user: currentUser, isLoading: authIsLoading } = useAuth();
  
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [userForModal, setUserForModal] = useState<User | null>(null);
  const [dataLoading, setDataLoading] = useState(true);

  const [userToResetPassword, setUserToResetPassword] = useState<User | null>(null);
  const [isResetPasswordModalOpen, setIsResetPasswordModalOpen] = useState(false);

  const fetchUsers = useCallback(async () => {
    setDataLoading(true);
    try {
      const data = await getUsers();
      setUsers(data);
    } catch (error) {
      console.error('Erro ao buscar usuários:', error);
      toast.error("Erro ao buscar usuários. Verifique o console para detalhes.");
    } finally {
      setDataLoading(false);
    }
  }, []);

  useEffect(() => {
    if (authIsLoading) {
      return; 
    }
    if (currentUser && currentUser.role === RoleEnum.Master) {
        fetchUsers();
    }
  }, [authIsLoading, currentUser, fetchUsers]);

  const handleDelete = (id: number) => {
    if (!currentUser) return;
    if (id === currentUser.id) {
      toast.warning("Você não pode excluir a si mesmo.");
      return;
    }

    toast.custom((t) => (
      <div className="bg-white dark:bg-zinc-950 p-4 rounded-md shadow-lg w-[380px] border border-white-500">
        <h3 className="text-lg font-semibold mb-2">Tem certeza que deseja excluir este usuário?</h3>
        <p className="text-sm text-muted-foreground mb-4">
          Esta ação não pode ser desfeita.
        </p>
        <div className="flex justify-end gap-2">
          <Button
            variant="ghost"
            onClick={() => toast.dismiss(t)}
          >
            Cancelar
          </Button>
          <Button
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={async () => {
              try {
                await deleteUser(id);
                toast.success("O usuário foi removido com sucesso.");
                // ✨ CORREÇÃO: Recarrega os dados para garantir consistência em vez de usar uma atualização otimista.
                fetchUsers();
              } catch (error: any) {
                const message = error?.response?.data?.message || "Erro ao excluir o usuário.";
                toast.error(`Erro ao excluir usuário: ${message}`);
              } finally {
                toast.dismiss(t);
              }
            }}
          >
            Confirmar Exclusão
          </Button>
        </div>
      </div>
    ), {
      position: "top-center",
      duration: Infinity
    });
  };

  const handleOpenCreateModal = () => {
    setUserForModal(null); 
    setModalMode('create');
  };

  const handleOpenEditModal = (userToEdit: User) => {
    setUserForModal(userToEdit);
    setModalMode('edit');
  };

  const handleOpenResetPasswordModal = (userToReset: User) => { 
    setUserToResetPassword(userToReset);
    setIsResetPasswordModalOpen(true);
  };

  const closeResetPasswordModal = () => { 
    setIsResetPasswordModalOpen(false);
    setUserToResetPassword(null);
  };

  const handlePasswordResetSuccess = () => { 
    // ✨ MELHORIA: Adiciona feedback de sucesso ao usuário.
    toast.success("Senha do usuário redefinida com sucesso!");
    closeResetPasswordModal();
  };

  const closeModal = () => {
    setModalMode(null);
    setUserForModal(null);
  };

  const handleSaveSuccess = () => {
    fetchUsers();
  };

  const columns: ColumnDef<User>[] = [
    {
      id: "select",
      header: ({ table }) => ( <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Select all" /> ),
      cell: ({ row }) => ( <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Select row" /> ),
      enableSorting: false,
      enableHiding: false,
    },
    { accessorKey: 'name', header: 'Nome' },
    { accessorKey: 'email', header: 'Email' },
    {
      accessorKey: 'role',
      header: 'Função',
      enableSorting: false,
      cell: ({ row }) => <div>{getRoleLabel(row.original.role)}</div>,
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      enableSorting: false,
      cell: ({ row }) => <div>{row.original.isActive ? 'Ativo' : 'Inativo'}</div>,
    },
    {
      id: "actions",
      header: () => <div className="text-right">Ações</div>,
      enableSorting: false,
      cell: ({ row }) => {
        const userRowData = row.original;
        // ✨ MELHORIA: Usa variável de ambiente para o e-mail protegido.
        const protectedUserEmail = 'helpdesk@pecege.com' || '';
        
        // ✨ CORREÇÃO: A comparação agora é robusta (ignora maiúsculas/minúsculas).
        const isProtected = userRowData.email.toLowerCase() === protectedUserEmail.toLowerCase();
        const isSelf = currentUser?.id === userRowData.id;

        return (
          <div className="text-right">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-8 w-8 p-0"><span className="sr-only">Open menu</span><MoreHorizontal className="h-4 w-4" /></Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Ações</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem 
                  onClick={() => !isProtected && handleOpenEditModal(userRowData)}
                  disabled={isProtected}
                  className={isProtected ? "text-muted-foreground cursor-not-allowed" : "cursor-pointer"}
                >
                  Editar
                </DropdownMenuItem>
                <DropdownMenuItem 
                  onClick={() => handleDelete(userRowData.id)}
                  disabled={isSelf || isProtected}
                  className={isSelf || isProtected ? "text-muted-foreground cursor-not-allowed" : "cursor-pointer"}
                >
                  Excluir
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => handleOpenResetPasswordModal(userRowData)}> 
                  Resetar Senha
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      }
    },
  ];

  if (authIsLoading) {
    return <div className="container mx-auto py-10 text-center">Carregando informações de autenticação...</div>;
  }

  if (!currentUser || currentUser.role !== RoleEnum.Master) {
    return <div className="container mx-auto py-10 text-center">Acesso não autorizado.</div>;
  }
  
  // ✨ CORREÇÃO: Removida a verificação de loading antiga. A DataTable agora cuidará disso.
  // if (dataLoading && users.length === 0) { ... }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Usuários</h1>
        <Button onClick={handleOpenCreateModal}>
          Criar Usuário
        </Button>
      </div>
      <DataTable
        columns={columns}
        data={users}
        // ✨ MELHORIA: Passa o estado de loading para a DataTable para um melhor feedback de UX.
        isLoading={dataLoading} 
        filterColumnId="email"
        filterPlaceholder="Filtrar por email do usuário..."
      />
      {modalMode && (
        <UserFormModal
          mode={modalMode}
          userToEdit={userForModal}
          isOpen={!!modalMode}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
        />
      )}
      <AdminResetPasswordModal
        userToReset={userToResetPassword}
        isOpen={isResetPasswordModalOpen}
        onClose={closeResetPasswordModal}
        onSuccess={handlePasswordResetSuccess}
      />
    </div>
  );
};

export default UsersPage;