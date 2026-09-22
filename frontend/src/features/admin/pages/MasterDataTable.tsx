import { FormEvent, useEffect, useState } from "react";
import {
  MasterDataError,
  createMasterDataItem,
  deleteMasterDataItem,
  getMasterDataList,
  updateMasterDataItem,
} from "../../../services/masterDataService";
import type { MasterDataEntity } from "../../../types/masterData";
import "../styles/AdminMasterDataPage.css";

export interface MasterDataColumn {
  key: string;
  label: string;
  type: "text" | "number" | "boolean";
}

interface BaseRecord {
  id: number;
  isActive: boolean;
  [key: string]: unknown;
}

interface MasterDataTableProps {
  title: string;
  entity: MasterDataEntity;
  columns: MasterDataColumn[]; // excludes "id"; include "isActive" explicitly if you want it editable
}

function emptyFormValues(columns: MasterDataColumn[]): Record<string, unknown> {
  const values: Record<string, unknown> = {};
  for (const col of columns) {
    if (col.key === "isActive") values[col.key] = true;
    else if (col.type === "number") values[col.key] = 0;
    else values[col.key] = "";
  }
  return values;
}

export default function MasterDataTable({ title, entity, columns }: MasterDataTableProps) {
  const [items, setItems] = useState<BaseRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formValues, setFormValues] = useState<Record<string, unknown>>(emptyFormValues(columns));
  const [formError, setFormError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  function loadItems() {
    setIsLoading(true);
    setLoadError(null);
    getMasterDataList<BaseRecord>(entity)
      .then(setItems)
      .catch((err) => setLoadError(err instanceof MasterDataError ? err.message : "Could not load data."))
      .finally(() => setIsLoading(false));
  }

  useEffect(loadItems, [entity]);

  function startAdd() {
    setIsAdding(true);
    setEditingId(null);
    setFormValues(emptyFormValues(columns));
    setFormError(null);
  }

  function startEdit(item: BaseRecord) {
    setEditingId(item.id);
    setIsAdding(false);
    setFormValues({ ...item });
    setFormError(null);
  }

  function cancelForm() {
    setIsAdding(false);
    setEditingId(null);
    setFormError(null);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);

    for (const col of columns) {
      if (col.type === "text" && !String(formValues[col.key] ?? "").trim()) {
        setFormError(`${col.label} is required.`);
        return;
      }
    }

    setIsSaving(true);
    try {
      if (isAdding) {
        await createMasterDataItem(entity, formValues);
      } else if (editingId !== null) {
        await updateMasterDataItem(entity, editingId, formValues);
      }
      setIsAdding(false);
      setEditingId(null);
      loadItems();
    } catch (err) {
      setFormError(err instanceof MasterDataError ? err.message : "Something went wrong. Please try again.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(id: number) {
    setDeleteError(null);
    setDeletingId(id);
    try {
      await deleteMasterDataItem(entity, id);
      loadItems();
    } catch (err) {
      const message =
        err instanceof MasterDataError ? err.message : "Could not delete this item.";
      setDeleteError(message);
    } finally {
      setDeletingId(null);
    }
  }

  const showForm = isAdding || editingId !== null;

  return (
    <div className="admin-master-data-table-section">

      <div className="admin-master-data-table-header">
        <h2>{title}</h2>

        {!showForm && (
          <button
            type="button"
            className="admin-master-data-add-button"
            onClick={startAdd}
          >
            + Add {title.slice(0, -1)}
          </button>
        )}
      </div>

      {deleteError && (
        <div
          role="alert"
          className="admin-master-data-error"
        >
          {deleteError}
        </div>
      )}

      {showForm && (
        <form
          onSubmit={handleSubmit}
          className="admin-master-data-form"
          noValidate
        >
          {columns.map((col) => (
            <div
              className="admin-master-data-form-group"
              key={col.key}
            >
              <label htmlFor={col.key}>
                {col.label}
              </label>

              {col.type === "boolean" ? (
                <input
                  id={col.key}
                  type="checkbox"
                  checked={Boolean(formValues[col.key])}
                  onChange={(e) =>
                    setFormValues((v) => ({
                      ...v,
                      [col.key]: e.target.checked,
                    }))
                  }
                />
              ) : (
                <input
                  id={col.key}
                  type={col.type === "number" ? "number" : "text"}
                  value={String(formValues[col.key] ?? "")}
                  onChange={(e) =>
                    setFormValues((v) => ({
                      ...v,
                      [col.key]:
                        col.type === "number"
                          ? Number(e.target.value)
                          : e.target.value,
                    }))
                  }
                />
              )}
            </div>
          ))}

          {formError && (
            <div
              role="alert"
              className="admin-master-data-error"
            >
              {formError}
            </div>
          )}

          <div className="admin-master-data-form-actions">
            <button
              type="submit"
              className="admin-master-data-save-button"
              disabled={isSaving}
            >
              {isSaving ? "Saving..." : "Save"}
            </button>

            <button
              type="button"
              className="admin-master-data-cancel-button"
              onClick={cancelForm}
              disabled={isSaving}
            >
              Cancel
            </button>
          </div>
        </form>
      )}

      {isLoading ? (
        <p
          role="status"
          className="admin-master-data-state"
        >
          Loading {title.toLowerCase()}...
        </p>
      ) : loadError ? (
        <div
          role="alert"
          className="admin-master-data-error"
        >
          {loadError}
        </div>
      ) : items.length === 0 ? (
        <p className="admin-master-data-empty">
          No {title.toLowerCase()} yet.
        </p>
      ) : (
        <div className="admin-master-data-table-wrapper">
          <table className="admin-master-data-table">
            <thead>
              <tr>
                {columns.map((col) => (
                  <th key={col.key}>
                    {col.label}
                  </th>
                ))}
                <th>Actions</th>
              </tr>
            </thead>

            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  {columns.map((col) => (
                    <td key={col.key}>
                      {col.type === "boolean"
                        ? item[col.key]
                          ? "Yes"
                          : "No"
                        : String(item[col.key])}
                    </td>
                  ))}

                  <td className="admin-master-data-actions">
                    <button
                      type="button"
                      className="admin-master-data-action-button"
                      onClick={() => startEdit(item)}
                    >
                      Edit
                    </button>

                    <button
                      type="button"
                      className="admin-master-data-action-button admin-master-data-delete-button"
                      onClick={() => handleDelete(item.id)}
                      disabled={deletingId === item.id}
                    >
                      {deletingId === item.id
                        ? "Deleting..."
                        : "Delete"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}