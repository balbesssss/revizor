import { useState } from 'react';
import styles from '../Role/RoleEdit.module.css';
import { apiFetch } from '../../api';

function PlanAddModal({ onSave, onClose }) {
    const [name, setName] = useState('');
    const [periodStart, setPeriodStart] = useState('');
    const [periodEnd, setPeriodEnd] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (name.trim().length < 10) {
            alert('Название должно содержать минимум 10 символов');
            return;
        }
        if (!periodStart) {
            alert('Укажите дату начала периода');
            return;
        }
        if (periodEnd && periodEnd < periodStart) {
            alert('Дата окончания не может быть раньше даты начала');
            return;
        }

        const body = { name, periodStart };
        if (periodEnd) body.periodEnd = periodEnd;

        const res = await apiFetch('http://localhost:5196/plan/', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            const listRes = await apiFetch('http://localhost:5196/plan', {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            const listData = await listRes.json();
            onSave(listData.data || []);
            onClose();
        } else {
            const err = await res.json().catch(() => null);
            alert(err?.message || 'Ошибка создания плана');
        }
    };

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h3>Создать план</h3>
                <form onSubmit={handleSubmit}>
                    <label>
                        Название:
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="Например: План на январь 2026"
                            autoFocus
                            required
                            minLength={10}
                            maxLength={50}
                        />
                    </label>
                    <label>
                        Начало периода:
                        <input
                            type="date"
                            value={periodStart}
                            onChange={(e) => setPeriodStart(e.target.value)}
                            required
                        />
                    </label>
                    <label>
                        Конец периода (необязательно):
                        <input
                            type="date"
                            value={periodEnd}
                            onChange={(e) => setPeriodEnd(e.target.value)}
                        />
                        <small>Если не указать — будет +1 месяц от начала</small>
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

export default PlanAddModal;