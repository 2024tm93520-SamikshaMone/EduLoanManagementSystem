import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotifications, markAllNotificationsRead, markNotificationRead, type AppNotification } from "../../services/notificationService";
import "./NotificationsPage.css";

export default function NotificationsPage() {
  const [items, setItems] = useState<AppNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const navigate = useNavigate();
  const load = useCallback(async () => {
    setLoading(true);
    try {
      setError("");
      const feed = await getNotifications();
      setItems(feed.items);
      setUnreadCount(feed.unreadCount);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not load notifications.");
    } finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);

  const markRead = async (item: AppNotification) => {
    if (!item.isRead) {
      try {
        await markNotificationRead(item.id);
        setItems(old => old.map(n => n.id === item.id ? { ...n, isRead: true, readAtUtc: new Date().toISOString() } : n));
        setUnreadCount(c => Math.max(0, c - 1));
        setError("");
      } catch (e) { setError(e instanceof Error ? e.message : "Unable to update notification."); return; }
    }
    if (item.link) navigate(item.link);
  };

  const markAll = async () => {
    try {
      await markAllNotificationsRead();
      setItems(old => old.map(n => ({ ...n, isRead: true, readAtUtc: n.readAtUtc ?? new Date().toISOString() })));
      setUnreadCount(0);
      setError("");
    } catch (e) { setError(e instanceof Error ? e.message : "Unable to update notifications."); }
  };

  return <main className="notifications-page">
    <header className="notifications-heading"><div><h1>Notifications</h1><p>Application updates and actions requiring your attention.</p></div>
      <div className="notifications-heading-actions"><button type="button" onClick={() => void load()} disabled={loading}>Refresh</button><button type="button" onClick={() => void markAll()} disabled={unreadCount === 0 || loading}>Mark all as read ({unreadCount})</button></div>
    </header>
    {error && <div role="alert" className="notifications-error"><span>{error}</span><button type="button" onClick={() => void load()}>Retry</button></div>}
    {loading ? <p role="status">Loading notifications…</p> : items.length === 0 ? <section className="notifications-empty">You're all caught up. No notifications to show.</section> : <section className="notifications-list">{items.map(item => <article key={item.id} className={`notification-item ${item.isRead ? "is-read" : "is-unread"}`}>
      <div className="notification-item-main"><div className="notification-item-meta"><span>{item.category}</span><time dateTime={item.createdAtUtc}>{new Date(item.createdAtUtc).toLocaleString()}</time></div><h2>{item.title}</h2><p>{item.message}</p></div>
      <button type="button" onClick={() => void markRead(item)}>{item.link ? "View" : item.isRead ? "Read" : "Mark read"}</button>
    </article>)}</section>}
  </main>;
}
