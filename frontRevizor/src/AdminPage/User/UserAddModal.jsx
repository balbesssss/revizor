import { useState } from 'react';
import styles from './UserModal.module.css';
import { apiFetch } from '../../api';

function UserAddModal({ user, roles = [], onSave, onClose }) {
    const isEdit = Boolean(user);

    const [firstName, setFirstName] = useState(user?.firstName ?? '');
    const [secondName, setSecondName] = useState(user?.secondName ?? '');
    const [lastName, setLastName] = useState(user?.lastName ?? '');
    const [roleId, setRoleId] = useState(user?.roleId ?? '');
    const [password, setPassword] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (
            firstName.trim() === '' ||
            lastName.trim() === '' ||
            roleId === '' ||
            (!isEdit && password.trim() === '')
        ) {
            alert('Заполните обязательные поля');
            return;
        }

        const body = {
            firstName: firstName.trim(),
            secondName: secondName.trim(),
            lastName: lastName.trim(),
            roleId: Number(roleId),
        };
        if (!isEdit || password.trim() !== '') {
            body.password = password;
        }

        const url = isEdit
            ? `http://localhost:5196/users/${user.id}`
            : 'http://localhost:5196/users';

        const res = await apiFetch(url, {
            method: isEdit ? 'PUT' : 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            onSave();
            onClose();
        } else {
            alert('Ошибка сохранения');
        }
    };

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h3>{isEdit ? 'Изменить пользователя' : 'Создать пользователя'}</h3>
                <form onSubmit={handleSubmit}>
                    <label>
                        Имя:
                        <input
                            type="text"
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            autoFocus
                            required
                        />
                    </label>
                    <label>
                        Отчество:
                        <input
                            type="text"
                            value={secondName}
                            onChange={(e) => setSecondName(e.target.value)}
                        />
                    </label>
                    <label>
                        Фамилия:
                        <input
                            type="text"
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            required
                        />
                    </label>
                    <label>
                        Роль:
                        {roles.length > 0 ? (
                            <select
                                value={roleId}
                                onChange={(e) => setRoleId(e.target.value)}
                                required
                            >
                                <option value="">— выберите роль —</option>
                                {roles.map(r => (
                                    <option key={r.id} value={r.id}>{r.name}</option>
                                ))}
                            </select>
                        ) : (
                            <input
                                type="number"
                                placeholder="roleId"
                                value={roleId}
                                onChange={(e) => setRoleId(e.target.value)}
                                required
                            />
                        )}
                    </label>
                    <label>
                        Пароль{isEdit ? ' (новый, необязательно)' : ''}:
                        <input
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required={!isEdit}
                        />
                    </label>
                    <div className={styles.buttons}>
                        <button type="button" onClick={onClose}>Отмена</button>
                        <button type="submit">Сохранить</button>
                    </div>
                </form>
            </div>
        </div>
    );
}

export default UserAddModal;