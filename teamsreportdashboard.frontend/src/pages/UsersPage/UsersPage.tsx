import React, { useEffect, useState } from 'react';
import { CustomTable } from '../../components/CustomTable/CustomTable';  // Importe seu componente de tabela
import { Button, Modal, Form } from 'react-bootstrap';
import { MdEditSquare, MdDelete } from 'react-icons/md';
import AxiosConfig from '../../services/axiosConfig'; // Para fazer as requisições à API

interface User {
    id: number;
    name: string;
    email: string;
    role: string;
    isActive: boolean;
}

const UsersPage: React.FC = () => {
    const [users, setUsers] = useState<User[]>([]);
    const [showModal, setShowModal] = useState(false);
    const [selectedUser, setSelectedUser] = useState<User | null>(null);

    useEffect(() => {
        // Função para carregar os usuários
        const fetchUsers = async () => {
            try {
                const response = await AxiosConfig.get('/user');
                setUsers(response.data);
            } catch (error) {
                console.error('Error fetching users:', error);
            }
        };

        fetchUsers();
    }, []);

    // Função para editar usuário
    const handleEdit = (user: User) => {
        setSelectedUser(user);
        setShowModal(true);
    };

    // Função para excluir usuário
    const handleDelete = async (user: User) => {
        if (window.confirm(`Are you sure you want to delete ${user.name}?`)) {
            try {
                await axios.delete(`/api/user?id=${user.id}`);
                setUsers(users.filter(u => u.id !== user.id)); // Remove o usuário da lista
            } catch (error) {
                console.error('Error deleting user:', error);
            }
        }
    };

    // Função para atualizar os dados do usuário
    const handleUpdateUser = async () => {
        if (!selectedUser) return;

        try {
            await axios.put('/api/user', selectedUser);  // Enviar dados para a API
            setShowModal(false);
            setSelectedUser(null);
            // Atualize a lista de usuários após a edição
            const updatedUsers = await axios.get('/api/user');
            setUsers(updatedUsers.data);
        } catch (error) {
            console.error('Error updating user:', error);
        }
    };

    // Definindo as colunas para a tabela
    const columns = [
        { title: 'Name', data: 'name' },
        { title: 'Email', data: 'email' },
        { title: 'Role', data: 'role' },
        { title: 'Status', data: 'isActive', render: (value: boolean) => (value ? 'Active' : 'Inactive') },
        { 
            title: 'Actions', 
            render: (_: any, row: User) => (
                <div>
                    <Button variant="warning" size="sm" onClick={() => handleEdit(row)}><MdEditSquare /></Button>
                    <Button variant="danger" size="sm" onClick={() => handleDelete(row)}><MdDelete /></Button>
                </div>
            )
        }
    ];

    return (
        <div className="user-page">
            <h2>Users</h2>
            <CustomTable
                data={users}
                columns={columns}
                actions={{
                    onEdit: handleEdit,
                    onDelete: handleDelete,
                }}
            />

            {/* Modal para editar o usuário */}
            <Modal show={showModal} onHide={() => setShowModal(false)}>
                <Modal.Header closeButton>
                    <Modal.Title>Edit User</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedUser && (
                        <Form>
                            <Form.Group className="mb-3">
                                <Form.Label>Name</Form.Label>
                                <Form.Control
                                    type="text"
                                    value={selectedUser.name}
                                    onChange={e => setSelectedUser({...selectedUser, name: e.target.value})}
                                />
                            </Form.Group>

                            <Form.Group className="mb-3">
                                <Form.Label>Email</Form.Label>
                                <Form.Control
                                    type="email"
                                    value={selectedUser.email}
                                    onChange={e => setSelectedUser({...selectedUser, email: e.target.value})}
                                />
                            </Form.Group>

                            <Form.Group className="mb-3">
                                <Form.Label>Role</Form.Label>
                                <Form.Control
                                    type="text"
                                    value={selectedUser.role}
                                    onChange={e => setSelectedUser({...selectedUser, role: e.target.value})}
                                />
                            </Form.Group>

                            <Form.Group className="mb-3">
                                <Form.Check
                                    type="checkbox"
                                    label="Active"
                                    checked={selectedUser.isActive}
                                    onChange={e => setSelectedUser({...selectedUser, isActive: e.target.checked})}
                                />
                            </Form.Group>
                        </Form>
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowModal(false)}>
                        Close
                    </Button>
                    <Button variant="primary" onClick={handleUpdateUser}>
                        Save Changes
                    </Button>
                </Modal.Footer>
            </Modal>
        </div>
    );
};

export default UsersPage;
