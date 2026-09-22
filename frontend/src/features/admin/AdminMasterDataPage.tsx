import { useState } from "react";
import MasterDataTable from "./MasterDataTable";
import EligibilityRulesTable from "./EligibilityRulesTable";

type Tab = "departments" | "colleges" | "courses" | "eligibilityRules";

const TABS: { id: Tab; label: string }[] = [
  { id: "departments", label: "Departments" },
  { id: "colleges", label: "Colleges" },
  { id: "courses", label: "Courses" },
  { id: "eligibilityRules", label: "Eligibility Rules" },
];

export default function AdminMasterDataPage() {
  const [activeTab, setActiveTab] = useState<Tab>("departments");

  return (
    <div className="admin-page">
      <div className="admin-card">
        <h1>Master Data Management</h1>

        <div className="admin-tabs" role="tablist">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={activeTab === tab.id}
              className={`admin-tab ${activeTab === tab.id ? "active" : ""}`}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {activeTab === "departments" && (
          <MasterDataTable
            title="Departments"
            entity="departments"
            columns={[
              { key: "name", label: "Name", type: "text" },
              { key: "isActive", label: "Active", type: "boolean" },
            ]}
          />
        )}

        {activeTab === "colleges" && (
          <MasterDataTable
            title="Colleges"
            entity="colleges"
            columns={[
              { key: "name", label: "Name", type: "text" },
              { key: "city", label: "City", type: "text" },
              { key: "isActive", label: "Active", type: "boolean" },
            ]}
          />
        )}

        {activeTab === "courses" && (
          <MasterDataTable
            title="Courses"
            entity="courses"
            columns={[
              { key: "name", label: "Name", type: "text" },
              { key: "level", label: "Level", type: "text" },
              { key: "standardDurationMonths", label: "Duration (months)", type: "number" },
              { key: "isActive", label: "Active", type: "boolean" },
            ]}
          />
        )}

        {activeTab === "eligibilityRules" && <EligibilityRulesTable />}
      </div>
    </div>
  );
}
