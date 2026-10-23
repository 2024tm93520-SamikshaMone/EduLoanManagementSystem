import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { downloadEmployeeDocument, getEmployeeDocuments, getEmployeeDocumentRequirements, uploadEmployeeDocument, type EmployeeDocument, type EmployeeDocumentRequirement } from "../../services/employeeDocumentService";
import "./DocumentsPage.css";

export default function DocumentsPage() {
  const [documents, setDocuments] = useState<EmployeeDocument[]>([]);
  const [requirements, setRequirements] = useState<EmployeeDocumentRequirement[]>([]);
  const [documentType, setDocumentType] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  async function refresh() {
    setLoading(true); setError(null);
    const docsResult = await getEmployeeDocuments().then(data => ({ data })).catch(e => ({ error: e }));
    if ("error" in docsResult) setError(docsResult.error instanceof Error ? docsResult.error.message : "Could not load documents.");
    else setDocuments(docsResult.data);
    const requirementsResult = await getEmployeeDocumentRequirements().then(data => ({ data })).catch(() => ({ data: [] as EmployeeDocumentRequirement[] }));
    setRequirements(requirementsResult.data);
    if (!documentType && requirementsResult.data.length) setDocumentType(requirementsResult.data[0].documentType);
    setLoading(false);
  }
  useEffect(() => { void refresh(); }, []);
  useEffect(() => {
    if (!documentType && requirements.length > 0) {
      setDocumentType(requirements[0].documentType);
    }
  }, [documentType, requirements]);
  const selectedDocumentType = documentType || requirements[0]?.documentType || "";
  async function onUpload(e: FormEvent) {
    e.preventDefault(); setError(null); setNotice(null);
    if (!selectedDocumentType) { setError("Choose a document type."); return; }
    if (!file) { setError("Choose a PDF, JPG, or PNG file."); return; }
    setBusy(true);
    try { const saved = await uploadEmployeeDocument(selectedDocumentType, file); setDocuments(current => [saved, ...current]); setFile(null); const input = document.getElementById("employee-document-file") as HTMLInputElement | null; if (input) input.value = ""; setNotice("Document uploaded to My Documents. You can reuse it in a loan application."); }
    catch (e) { setError(e instanceof Error ? e.message : "Upload failed."); } finally { setBusy(false); }
  }
  async function onDownload(doc: EmployeeDocument) { setError(null); try { await downloadEmployeeDocument(doc.id, doc.fileName); } catch (e) { setError(e instanceof Error ? e.message : "Download failed."); } }
  return <main className="employee-documents-page"><section className="employee-documents-card">
    <div className="employee-documents-heading"><div><span className="employee-documents-eyebrow">EMPLOYEE PORTAL</span><h1>My Documents</h1><p>Keep your supporting documents in one place and reuse them when applying for a loan.</p></div><Link to="/employee/applications/new" className="employee-documents-primary-link">Start a loan application</Link></div>
    <form className="employee-documents-upload" onSubmit={onUpload}><h2>Upload a document</h2><p>PDF, JPG, JPEG, or PNG · Maximum 5 MB per file.</p>
      <label htmlFor="employee-document-type">Document type</label><select id="employee-document-type" value={selectedDocumentType} onChange={e => setDocumentType(e.target.value)}><option value="">Select document type</option>{requirements.map(r => <option key={r.id} value={r.documentType}>{r.displayName}{r.isRequired ? " (Required)" : " (Optional)"}</option>)}<option value="Other">Other supporting document</option></select>
      <label htmlFor="employee-document-file">Choose file</label><input id="employee-document-file" type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setFile(e.target.files?.[0] ?? null)} />
      <button type="submit" disabled={busy || loading}>{busy ? "Uploading…" : "Upload to My Documents"}</button>
    </form>
    {error && <div className="employee-documents-alert error" role="alert">{error}</div>}{notice && <div className="employee-documents-alert success" role="status">{notice}</div>}
    <div className="employee-documents-list-heading"><div><h2>Your saved documents</h2><p>{documents.length} document{documents.length === 1 ? "" : "s"} in your library</p></div><button type="button" className="employee-documents-refresh" onClick={() => void refresh()} disabled={loading}>Refresh</button></div>
    {loading ? <p className="employee-documents-empty">Loading documents…</p> : documents.length === 0 ? <div className="employee-documents-empty"><h3>No documents uploaded yet</h3><p>Upload your documents once. They will be available to select during loan application.</p></div> : <div className="employee-documents-table-wrap"><table className="employee-documents-table"><thead><tr><th>Document</th><th>File name</th><th>Size</th><th>Uploaded</th><th>Actions</th></tr></thead><tbody>{documents.map(doc => <tr key={doc.id}><td><span className="employee-documents-type">{doc.documentType}</span></td><td>{doc.fileName}</td><td>{(doc.fileSizeBytes / 1024).toFixed(0)} KB</td><td>{new Date(doc.uploadedAtUtc).toLocaleDateString()}</td><td><button type="button" className="employee-documents-action" onClick={() => void onDownload(doc)}>Download</button></td></tr>)}</tbody></table></div>}
  </section></main>;
}
