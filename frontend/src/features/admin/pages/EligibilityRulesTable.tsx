import { FormEvent, useEffect, useState } from "react";
import {
  CONDITION_FIELDS,
  EligibilityRule,
  EligibilityRuleError,
  EligibilityRuleFormValues,
  OPERATORS,
  createEligibilityRule,
  deleteEligibilityRule,
  getEligibilityRules,
  updateEligibilityRule,
} from "../../../services/eligibilityRuleService";
import "../styles/AdminMasterDataPage.css";

const EMPTY_FORM: EligibilityRuleFormValues = {
  ruleName: "",
  ruleCategory: "",
  conditionField: CONDITION_FIELDS[0],
  operator: OPERATORS[0],
  conditionValue: 0,
  errorMessage: "",
  severity: "Blocking",
  isActive: true,
};

export default function EligibilityRulesTable() {
  const [rules, setRules] = useState<EligibilityRule[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formValues, setFormValues] = useState<EligibilityRuleFormValues>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  function loadRules() {
    setIsLoading(true);
    setLoadError(null);
    getEligibilityRules()
      .then(setRules)
      .catch((err) => setLoadError(err instanceof EligibilityRuleError ? err.message : "Could not load rules."))
      .finally(() => setIsLoading(false));
  }

  useEffect(loadRules, []);

  function startAdd() {
    setIsAdding(true);
    setEditingId(null);
    setFormValues(EMPTY_FORM);
    setFormError(null);
  }

  function startEdit(rule: EligibilityRule) {
    setEditingId(rule.id);
    setIsAdding(false);
    setFormValues({ ...rule });
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

    if (!formValues.ruleName.trim()) return setFormError("Rule name is required.");
    if (!formValues.ruleCategory.trim()) return setFormError("Category is required.");
    if (!formValues.errorMessage.trim()) return setFormError("Error message is required.");

    setIsSaving(true);
    try {
      if (isAdding) {
        await createEligibilityRule(formValues);
      } else if (editingId !== null) {
        await updateEligibilityRule(editingId, formValues);
      }
      setIsAdding(false);
      setEditingId(null);
      loadRules();
    } catch (err) {
      setFormError(err instanceof EligibilityRuleError ? err.message : "Something went wrong. Please try again.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(id: number) {
    setDeleteError(null);
    setDeletingId(id);
    try {
      await deleteEligibilityRule(id);
      loadRules();
    } catch (err) {
      setDeleteError(err instanceof EligibilityRuleError ? err.message : "Could not delete this rule.");
    } finally {
      setDeletingId(null);
    }
  }

  const showForm = isAdding || editingId !== null;

  return (
    <div className="admin-master-data-table-section">

      <div className="admin-master-data-table-header">
        <h2>Eligibility Rules</h2>

        {!showForm && (
          <button
            type="button"
            className="admin-master-data-add-button"
            onClick={startAdd}
          >
            + Add Rule
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
          className="admin-master-data-form admin-master-data-rule-form"
          noValidate
        >
          <div className="admin-master-data-form-group">
            <label htmlFor="ruleName">Rule Name</label>
            <input
              id="ruleName"
              value={formValues.ruleName}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  ruleName: e.target.value,
                }))
              }
            />
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="ruleCategory">Category</label>
            <input
              id="ruleCategory"
              placeholder="e.g. Tenure, Salary, LoanCap"
              value={formValues.ruleCategory}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  ruleCategory: e.target.value,
                }))
              }
            />
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="conditionField">Field</label>
            <select
              id="conditionField"
              value={formValues.conditionField}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  conditionField: e.target.value,
                }))
              }
            >
              {CONDITION_FIELDS.map((field) => (
                <option key={field} value={field}>
                  {field}
                </option>
              ))}
            </select>
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="operator">Operator</label>
            <select
              id="operator"
              value={formValues.operator}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  operator: e.target.value,
                }))
              }
            >
              {OPERATORS.map((operator) => (
                <option key={operator} value={operator}>
                  {operator}
                </option>
              ))}
            </select>
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="conditionValue">Value</label>
            <input
              id="conditionValue"
              type="number"
              value={formValues.conditionValue}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  conditionValue: Number(e.target.value),
                }))
              }
            />
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="errorMessage">
              Error Message (shown to employee)
            </label>

            <input
              id="errorMessage"
              value={formValues.errorMessage}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  errorMessage: e.target.value,
                }))
              }
            />
          </div>

          <div className="admin-master-data-form-group">
            <label htmlFor="severity">Severity</label>

            <select
              id="severity"
              value={formValues.severity}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  severity: e.target.value as
                    | "Blocking"
                    | "Warning",
                }))
              }
            >
              <option value="Blocking">
                Blocking (prevents submission)
              </option>

              <option value="Warning">
                Warning (recorded only)
              </option>
            </select>
          </div>

          <div className="admin-master-data-form-group admin-master-data-checkbox-group">
            <label htmlFor="isActive">Active</label>

            <input
              id="isActive"
              type="checkbox"
              checked={formValues.isActive}
              onChange={(e) =>
                setFormValues((v) => ({
                  ...v,
                  isActive: e.target.checked,
                }))
              }
            />
          </div>

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
          Loading eligibility rules...
        </p>
      ) : loadError ? (
        <div
          role="alert"
          className="admin-master-data-error"
        >
          {loadError}
        </div>
      ) : rules.length === 0 ? (
        <p className="admin-master-data-empty">
          No eligibility rules yet.
        </p>
      ) : (
        <div className="admin-master-data-table-wrapper">
          <table className="admin-master-data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Category</th>
                <th>Condition</th>
                <th>Severity</th>
                <th>Active</th>
                <th>Actions</th>
              </tr>
            </thead>

            <tbody>
              {rules.map((rule) => (
                <tr key={rule.id}>
                  <td>{rule.ruleName}</td>
                  <td>{rule.ruleCategory}</td>

                  <td>
                    {rule.conditionField} {rule.operator}{" "}
                    {rule.conditionValue}
                  </td>

                  <td>{rule.severity}</td>

                  <td>
                    {rule.isActive ? "Yes" : "No"}
                  </td>

                  <td className="admin-master-data-actions">
                    <button
                      type="button"
                      className="admin-master-data-action-button"
                      onClick={() => startEdit(rule)}
                    >
                      Edit
                    </button>

                    <button
                      type="button"
                      className="admin-master-data-action-button admin-master-data-delete-button"
                      onClick={() => handleDelete(rule.id)}
                      disabled={deletingId === rule.id}
                    >
                      {deletingId === rule.id
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
