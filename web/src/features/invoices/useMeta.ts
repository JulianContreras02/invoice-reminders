import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { Meta } from '../../api/types';

/** Datos de entorno que no cambian durante la sesión (enlace a Mailpit, si se permite reiniciar). */
export function useMeta() {
  const [meta, setMeta] = useState<Meta | null>(null);

  useEffect(() => {
    let stale = false;
    api
      .getMeta()
      .then((next) => {
        if (!stale) setMeta(next);
      })
      // Sin meta la página sigue funcionando: solo se ocultan el enlace a Mailpit y el botón de reinicio.
      .catch(() => undefined);
    return () => {
      stale = true;
    };
  }, []);

  return meta;
}
