import { useState } from 'react';
import styles from '../Role/RoleEdit.module.css';
import { apiFetch } from '../../api';

function PlanEditModal({ plan, onSave, onClose }) {
    const [name, setName] = useState(plan.name);
    const [periodStart, setPeriodStart] = useState(plan.periodStart?.split('T')[0] || '');
    const [periodEnd, setPeriodEnd] = useState(plan.periodEnd?.split('T')[0] || '');

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (name.trim() === '') {
            alert('Название не может быть пустым');
            return;
        }

        const body = { name };
        if (periodStart) body.periodStart = periodStart;
        if (periodEnd) body.periodEnd = periodEnd;

        const res = await apiFetch(`http://localhost:5196/plan/${plan.id}`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            onSave({ ...plan, name, periodStart: body.periodStart || plan.periodStart, periodEnd: body.periodEnd || plan.periodEnd });
            onClose();
        } else {
            alert('Ошибка сохранения');
        }
    };

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h3>Изменить план</h3>
                <form onSubmit={handleSubmit}>
                    <label>
                        Название:
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            autoFocus
                            required
                        />
                    </label>
                    <label>
                        Начало периода:
                        <input
                            type="date"
                            value={periodStart}
                            onChange={(e) => setPeriodStart(e.target.value)}
                        />
                    </label>
                    <label>
                        Конец периода:
                        <input
                            type="date"
                            value={periodEnd}
                            onChange={(e) => setPeriodEnd(e.target.value)}
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

export default PlanEditModal;