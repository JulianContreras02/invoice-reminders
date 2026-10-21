import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { InvoicesPage } from './features/invoices/InvoicesPage';
import './styles.css';

const root = document.getElementById('root');
if (!root) {
  throw new Error('No se encontró el elemento #root.');
}

createRoot(root).render(
  <StrictMode>
    <InvoicesPage />
  </StrictMode>,
);
