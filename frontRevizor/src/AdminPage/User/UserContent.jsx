import { useEffect, useState } from "react";
import styles from '../Role/RolePage.module.css';
import { apiFetch } from "../../api";
import UserAddModal from './UserAddModal';

function UserContent() {
    const [users, setUsers] = useState([]);
    const [roles, setRoles] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedUser, setSelectedUser] = useState(null);
    const [modalOpen, setModalOpen] = useState(false);
    const [editingUser, setEditingUser] = useState(null); // null = создание

    const authHeaders = () => ({
        'Authorization': `Bearer ${localStorage.getItem('token')}`
    });

    const loadUsers = () => {
        setLoading(true);
        apiFetch("http://localhost:5196/users", { headers: authHeaders() })
            .then(res => res.json())
            .then(data => {
                setUsers(data.data);
                setLoading(false);
            })
            .catch(() => setLoading(false));
    };

    const loadRoles = () => {
        apiFetch("http://localhost:5196/roles", { headers: authHeaders() })
            .then(res => res.json())
            .then(data => setRoles(data.data ?? data))
            .catch(() => setRoles([]));
    };

    useEffect(() => {
        loadUsers();
        loadRoles();
    }, []);

    const openAdd = () => {
        setEditingUser(null);
        setModalOpen(true);
    };

    const openEdit = () => {
        if (!selectedUser) return;
        setEditingUser(selectedUser);
        setModalOpen(true);
    };

    const handleDelete = async (id) => {
        if (!window.confirm('Удалить пользователя?')) return;
        const res = await apiFetch(`http://localhost:5196/users/${id}`, {
            method: 'DELETE',
            headers: authHeaders()
        });
        if (res.ok) {
            setSelectedUser(null);
            loadUsers();
        } else {
            alert('Ошибка удаления');
        }
    };

    if (loading) return <h1>Загрузка...</h1>;

    return (
        <div>
            <div className={styles.header}>
                <h2>Пользователи</h2>
                <button onClick={openAdd}>+ Добавить</button>
            </div>

            {selectedUser && (
                <div className={styles.actions}>
                    <span>Выбрано: {selectedUser.fullName}</span>
                    <button onClick={openEdit}>Изменить</button>
                    <button onClick={() => handleDelete(selectedUser.id)}>Удалить</button>
                    <button onClick={() => setSelectedUser(null)}>Отмена</button>
                </div>
            )}

            <table className={styles.table}>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>ФИО</th>
                        <th>Роль</th>
                    </tr>
                </thead>
                <tbody>
                    {users.map(user => (
                        <tr
                            key={user.id}
                            onClick={() => setSelectedUser(user)}
                            className={selectedUser?.id === user.id ? styles.selected : ''}
                        >
                            <td>{user.id}</td>
                            <td>{user.fullName}</td>
                            <td>{user.role}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {modalOpen && (
                <UserAddModal
                    user={editingUser}
                    roles={roles}
                    onSave={loadUsers}
                    onClose={() => {
                        setModalOpen(false);
                        setEditingUser(null);
                    }}
                />
            )}
        </div>
    );
}

export default UserContent;