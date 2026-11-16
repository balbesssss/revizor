import {useEffect, useState } from "react";
import styles from '../Role/RolePage.module.css'
import { apiFetch } from "../../api";


function UserContent() {
    const [user, setUser] = useState([])
    const [loading, setLoading] = useState(true);
    const [selectUser, setSelectUser] = useState(null)

   useEffect(() => {
    apiFetch("http://localhost:5196/users", {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    }).then(res=> res.json()).then(data=> setUser(data.data),setLoading(false));
  }, []);

    if (loading) return <h1>Загрузка...</h1>;

    return (
        <div>
            <div className={styles.header}>
            <h2>Пользователи</h2>
            <button>+ Добавить</button>
            </div>
            {selectUser && (
                <div className={styles.actions}>
                <span>Выбрано: {selectUser.fullName}</span>
                <button onClick={() => (selectUser)}>Изменить</button>
                <button onClick={() => handleDelete(selectedUser.id)}>Удалить</button>
                <button onClick={() => setSelectUser(null)}>Отмена</button>
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
                {user.map(user => (
                <tr
                    key={user.id}
                    onClick={() => setSelectUser(user)}
                >
                    <td>{user.id}</td>
                    <td>{user.fullName}</td>
                    <td>{user.role}</td>
                </tr>
                ))}
            </tbody>
            </table>
        </div>
    )
}

export default UserContent;