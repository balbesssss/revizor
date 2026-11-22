import { useEffect, useState } from 'react';
import styles from '../Role/RoleEdit.module.css';
import { apiFetch } from '../../api';

function AssignmentAddModal({ planId, onSave, onClose }) {
    const [facilities, setFacilities] = useState([]);
    const [users, setUsers] = useState([]);
    const [facilityId, setFacilityId] = useState('');
    const [passAt, setPassAt] = useState('');
    const [passById, setPassById] = useState('');
    const [responsiblePersonId, setResponsiblePersonId] = useState('');

    useEffect(() => {
        apiFetch('http://localhost:5196/facility', {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        }).then(res => res.json()).then(data => setFacilities(data.data || []));

        apiFetch('http://localhost:5196/users', {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        }).then(res => res.json()).then(data => setUsers(data.data || []));
    }, []);

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!facilityId || !passAt || !passById) {
            alert('Заполните объект, время и ревизора');
            return;
        }

        const assignment = {
            facilityId,
            passAt: new Date(passAt).toISOString(),
            passById,
            responsiblePersonId: responsiblePersonId || null
        };

        const res = await apiFetch(`http://localhost:5196/plan-assigment/${planId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify([assignment])
        });

        if (res.ok) {
            onSave();
            onClose();
        } else {
            const err = await res.json().catch(() => null);
            alert(err?.message || 'Ошибка сохранения');
        }
    };

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h3>Добавить назначение</h3>
                <form onSubmit={handleSubmit}>
                    <label>
                        Объект:
                        <select value={facilityId} onChange={(e) => setFacilityId(e.target.value)} required>
                            <option value="">— выберите —</option>
                            {facilities.map(f => (
                                <option key={f.id} value={f.id}>{f.title} ({f.location})</option>
                            ))}
                        </select>
                    </label>
                    <label>
                        Дата и время:
                        <input
                            type="datetime-local"
                            value={passAt}
                            onChange={(e) => setPassAt(e.target.value)}
                            required
                        />
                    </label>
                    <label>
                        Ревизор:
                        <select value={passById} onChange={(e) => setPassById(e.target.value)} required>
                            <option value="">— выберите —</option>
                            {users.map(u => (
                                <option key={u.id} value={u.id}>{u.fullName} ({u.role})</option>
                            ))}
                        </select>
                    </label>
                    <label>
                        Ответственное лицо (необязательно):
                        <select value={responsiblePersonId} onChange={(e) => setResponsiblePersonId(e.target.value)}>
                            <option value="">— не назначено —</option>
                            {users.map(u => (
                                <option key={u.id} value={u.id}>{u.fullName} ({u.role})</option>
                            ))}
                        </select>
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

export default AssignmentAddModal;