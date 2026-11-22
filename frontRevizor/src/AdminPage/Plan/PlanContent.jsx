import { useEffect, useState } from "react";
import styles from '../Role/RolePage.module.css';
import { apiFetch } from "../../api";
import PlanEditModal from "./PlanEditModal";
import PlanAddModal from "./PlanAddModal";
import AssignmentAddModal from "./AssigmentAddModal";

function PlanContent() {
    const [plan, setPlan] = useState([]);
    const [selectedPlan, setSelectedPlan] = useState(null);
    const [isSelect, setIsSelect] = useState(false);
    const [dataSelectedPlan, setDataSelectedPlan] = useState([]);
    const [isEditOpen, setIsEditOpen] = useState(false);
    const [isAddPlanOpen, setIsAddPlanOpen] = useState(false);
    const [isAddAssignmentOpen, setIsAddAssignmentOpen] = useState(false);

    useEffect(() => {
        if (!selectedPlan) return;
        apiFetch(`http://localhost:5196/plan-assigment/${selectedPlan.id}`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        })
            .then(res => res.json())
            .then(data => setDataSelectedPlan(data.data || []));
    }, [selectedPlan]);

    useEffect(() => {
        apiFetch("http://localhost:5196/plan", {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        })
            .then(res => res.json())
            .then(data => setPlan(data.data || []));
    }, []);

    const handlePlanSaved = (updatedPlan) => {
        setPlan(prev => prev.map(p => p.id === updatedPlan.id ? updatedPlan : p));
        setSelectedPlan(updatedPlan);
        setIsEditOpen(false);
    };

    const handlePlanAdded = (updatedList) => {
        setPlan(updatedList);
        setIsAddPlanOpen(false);
    };

    const handleAssignmentAdded = async () => {
        setIsAddAssignmentOpen(false);
        if (!selectedPlan) return;
        const res = await apiFetch(`http://localhost:5196/plan-assigment/${selectedPlan.id}`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        const data = await res.json();
        setDataSelectedPlan(data.data || []);
    };

    const handleArchive = async () => {
        if (!confirm(`Архивировать план «${selectedPlan.name}»?`)) return;
        const res = await apiFetch('http://localhost:5196/plan/archived', {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        if (res.ok) {
            const listRes = await apiFetch('http://localhost:5196/plan', {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            const listData = await listRes.json();
            setPlan(listData.data || []);
            setSelectedPlan(prev => prev ? { ...prev, isArchived: true } : null);
        } else {
            alert('Ошибка архивирования');
        }
    };

    if (isSelect && selectedPlan) {
        return (
            <div>
                <button onClick={() => { setIsSelect(false); setSelectedPlan(null); }}>← Назад к списку</button>
                <div className={styles.header}>
                    <h1>{selectedPlan.name} {selectedPlan.isArchived && '(архивный)'}</h1>
                    <div>
                        <button onClick={() => setIsEditOpen(true)}>Изменить план</button>
                        <button onClick={() => setIsAddAssignmentOpen(true)}>+ Добавить назначение</button>
                        {!selectedPlan.isArchived && (
                            <button onClick={handleArchive}>Архивировать</button>
                        )}
                    </div>
                </div>

                {dataSelectedPlan.length === 0 ? (
                    <h2>Назначений пока нет</h2>
                ) : (
                    <table className={styles.table}>
                        <thead>
                            <tr>
                                <th>Объект</th>
                                <th>Начало обхода</th>
                                <th>Ревизоры</th>
                                <th>Ответственный</th>
                            </tr>
                        </thead>
                        <tbody>
                            {dataSelectedPlan.map(dp => (
                                <tr key={dp.facilityId || dp.id}>
                                    <td>{dp.facility}</td>
                                    <td>{new Date(dp.passAt).toLocaleString('ru-RU')}</td>
                                    <td>
                                        <ul>
                                            {dp.revizors?.map(rev => (
                                                <li key={rev.id}>{rev.fullName}</li>
                                            ))}
                                        </ul>
                                    </td>
                                    <td>{dp.responsiblePerson?.fullName || 'Не назначен'}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}

                {isEditOpen && (
                    <PlanEditModal plan={selectedPlan} onSave={handlePlanSaved} onClose={() => setIsEditOpen(false)} />
                )}
                {isAddAssignmentOpen && (
                    <AssignmentAddModal planId={selectedPlan.id} onSave={handleAssignmentAdded} onClose={() => setIsAddAssignmentOpen(false)} />
                )}
            </div>
        );
    }

    return (
        <div>
            <div className={styles.header}>
                <h2>Планы</h2>
                <button onClick={() => setIsAddPlanOpen(true)}>+ Добавить</button>
            </div>

            <table className={styles.table}>
                <thead>
                    <tr>
                        <th>Название</th>
                        <th>Дата создания</th>
                        <th>Начало</th>
                        <th>Конец</th>
                        <th>Архивный</th>
                    </tr>
                </thead>
                <tbody>
                    {plan.map(p => (
                        <tr key={p.id} onClick={() => { setIsSelect(true); setSelectedPlan(p); }}>
                            <td>{p.name}</td>
                            <td>{new Date(p.createdAt).toLocaleDateString('ru-RU')}</td>
                            <td>{p.periodStart}</td>
                            <td>{p.periodEnd}</td>
                            <td>{p.isArchived ? 'да' : 'нет'}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {isAddPlanOpen && (
                <PlanAddModal onSave={handlePlanAdded} onClose={() => setIsAddPlanOpen(false)} />
            )}
        </div>
    );
}

export default PlanContent;