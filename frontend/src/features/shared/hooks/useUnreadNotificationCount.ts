import { useCallback, useEffect, useState } from "react";
import { getUnreadNotificationCount } from "../../../services/notificationService";

/** Loads the authenticated user's unread count and refreshes when the tab regains focus. */
export function useUnreadNotificationCount() {
  const [count, setCount] = useState(0);
  const refresh = useCallback(async () => {
    try { setCount(await getUnreadNotificationCount()); }
    catch { /* Notification count is non-critical; the notification center exposes retryable errors. */ }
  }, []);
  useEffect(() => {
    void refresh();
    const onFocus = () => { void refresh(); };
    window.addEventListener("focus", onFocus);
    return () => window.removeEventListener("focus", onFocus);
  }, [refresh]);
  return { unreadCount: count, refreshUnreadCount: refresh };
}
