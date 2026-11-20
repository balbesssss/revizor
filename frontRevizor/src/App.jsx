import { Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './LoginPage';
import AdminPage from './AdminPage/AdminPage';
import RevizorPage from './RevizorPage/RevizorPage';
import RecordPage from './RevizorPage/RecordPage';
import ReportsPage from './RevizorPage/ReportsPage';
import ProtectedRoute from './ProtectedRoute';

function getUserFromCookie() {
  const match = document.cookie.split('; ').find(row => row.startsWith('userInfo='));
  if (!match) return null;
  const base64 = match.split('=')[1];
  try {
    console.log(JSON.parse(atob(base64)))
    return JSON.parse(atob(base64));
  } catch {
    return null;
  }
}

function RoleRedirect() {
  const user = getUserFromCookie();
  if (!user) return <Navigate to="/login" replace />;
  const role = (user.Role || '').toLowerCase();
  if (role === 'admin') return <Navigate to="/admin" replace />;
  if (role === 'revizor') return <Navigate to="/revizor" replace />;
  return <Navigate to="/login" replace />;
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/admin"
        element={
          <ProtectedRoute role="admin">
            <AdminPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/revizor"
        element={
          <ProtectedRoute role="revizor">
            <RevizorPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/record/:assigmentId"
        element={
          <ProtectedRoute role="revizor">
            <RecordPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/reports"
        element={
          <ProtectedRoute role="revizor">
            <ReportsPage />
          </ProtectedRoute>
        }
      />

      <Route path="/" element={<RoleRedirect />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;