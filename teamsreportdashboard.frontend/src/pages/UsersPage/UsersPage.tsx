import React, { useEffect, useState } from 'react';
import {getCurrentUser} from '../../utils/auth'; // Função para obter o usuário atual
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '../../components/CustomTable/DataTable'; // Importe seu componente de tabela
import { MoreHorizontal } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox"
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useNavigate } from 'react-router-dom';
import { EditUserModal } from '@/components/EditUserModal';
import { getUsers, deleteUser } from '@/services/userService';
import { toast } from 'sonner';



interface User {
  id: number;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
}

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const currentUser = getCurrentUser();
  const currentUserId = Number(currentUser?.id)
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {

    if(!currentUser) {
      navigate("/login");
      return;
    }
    // Função para carregar os usuários
    const fetchUsers = async () => {
      try {
        const data = await getUsers();
        setUsers(data);
      } catch (error) {
        console.error('Erro ao buscar usuários:', error);
        toast("Erro ao buscar usuários: " + error);
      }
    };

    fetchUsers();
  }, []);

    const handleDelete = async (id: number) => {
    if (id === currentUserId) {
      toast("Você não pode excluir a si mesmo.");
      return;
    }

    const confirmed = window.confirm("Você tem certeza que deseja excluir esse usuário?");
    if (!confirmed) return;

    try {
      await deleteUser(id);
      setUsers(prev => prev.filter(user => user.id !== id));
      toast("O usuário foi removido com sucesso.");
    } catch (error: any) {
      const message = error?.response?.data?.message || "Erro ao excluir o usuário.";
      toast("Erro ao excluir usuário: " + message);
      console.error("Erro ao excluir usuário:", error);
    }
  };

const handleEdit = (user: User) => {
  setEditingUser(user);
  setIsModalOpen(true);
};

const handleUserUpdate = (updatedUser: User) => {
  setUsers(prev =>
    prev.map(user => (user.id === updatedUser.id ? updatedUser : user))
  );
};

  const columns: ColumnDef<User>[] = [
    {
    id: "select",
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected() ||
          (table.getIsSomePageRowsSelected() && "indeterminate")
        }
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Select all"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label="Select row"
      />
    ),
    enableSorting: false,
    enableHiding: false,
  },
    {
      accessorKey: 'name',
      header: 'Nome',
      cell: ({ row }) => <div>{row.getValue('name')}</div>,
    },
    {
      accessorKey: 'email',
      header: 'Email',
      cell: ({ row }) => <div>{row.getValue('email')}</div>,
    },
    {
      accessorKey: 'role',
      header: 'Função',
      cell: ({ row }) => {
        const roleMap: Record<string, string> = {
          0: "Master",
          1: "Admin",
          2: "Viewer"
        };
        const role = row.getValue('role');
        return <div>{roleMap[role as string] || "Desconhecido"}</div>;
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => <div>{row.getValue('isActive') ? 'Ativo' : 'Inativo'}</div>,
    },
    {
      id: "actions",
      cell: ({ row }) => {
        const user = row.original;
        
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">                
                <span className="sr-only">Open menu</span>
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Actions</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => handleEdit(user)}>Editar</DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleDelete(user.id)}>Delete</DropdownMenuItem> 
            </DropdownMenuContent>
          </DropdownMenu>
        );
      }
    },
    
  ];

  return (
    <div className='container mx-auto py-10'>
      <DataTable
        columns={columns}
        data={users}
      />
      <EditUserModal
        user={editingUser}
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingUser(null);
        }}
        onSave={handleUserUpdate}
      />
    </div>
  );
};

export default UsersPage;
