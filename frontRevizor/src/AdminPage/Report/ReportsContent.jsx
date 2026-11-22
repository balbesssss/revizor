import { useEffect, useState } from 'react';
import styles from './ReportContent.module.css';
import { apiFetch } from '../../api';

function ReportContent() {
  const [report, setReport] = useState([]);
  const [selectReport, setSelectReport] = useState(null);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [plan, setPlan] = useState([]);

  useEffect(() => {
    apiFetch('http://localhost:5196/plan')
      .then(res => res.json())
      .then(data => setPlan(data.data || []));
  }, []);

  useEffect(() => {
    apiFetch('http://localhost:5196/reports')
      .then(res => res.json())
      .then(data => setReport(data.data || []))
      .catch(err => console.error(err));
  }, []);


  async function downloadReport(planId) {
    const res = await apiFetch(`http://localhost:5196/reports/${planId}/download`);
    
    if (!res.ok) throw new Error('Ошибка скачивания');
    
    const contentDisposition = res.headers.get('Content-Disposition');
    let fileName = `report_${planId}.xlsx`;
    if (contentDisposition) {
      const match = contentDisposition.match(/filename="?([^";]+)"?/);
      if (match) fileName = match[1];
    }

    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  async function generateReport(planId) {
    try {
      const res = await apiFetch(`http://localhost:5196/reports/${planId}/generate`, {
        method: 'GET'
      });
      
      if (!res.ok) throw new Error('Ошибка генерации');
      
      const reportsRes = await apiFetch('http://localhost:5196/reports');
      const data = await reportsRes.json();
      setReport(data.data || []);
      
      setIsMenuOpen(false);
    } catch (err) {
      console.error(err);
    }
  }


  return (
    <div>
      <div className={styles.header}>
        <h1>Отчеты</h1>
        
        <div className={styles.menuWrapper}>
          <button 
            className={styles.menuButton}
            onClick={() => setIsMenuOpen(!isMenuOpen)}
          >
            Сгенерировать отчет
          </button>
          
          {isMenuOpen && (
            <div className={styles.dropdownMenu}>
              {plan.length === 0 ? (
                <p className={styles.empty}>Нет планов</p>
              ) : (
                plan.map(p => (
                  <button
                    key={p.id}
                    className={styles.menuItem}
                    onClick={() => generateReport(p.id)}
                  >
                    {p.name}
                  </button>
                ))
              )}
            </div>
          )}
        </div>
      </div>

      <div className={styles.buttonContainer}>
        {report.length === 0 ? (
          <h1>Отчетов еще нет</h1>
        ) : (
          report.map(element => (
            <button
              key={element.planId}
              className={styles.button}
              onClick={() => downloadReport(element.id)}
            >
              {element.name}
            </button>
          ))
        )}
      </div>
    </div>
  );
}

export default ReportContent;