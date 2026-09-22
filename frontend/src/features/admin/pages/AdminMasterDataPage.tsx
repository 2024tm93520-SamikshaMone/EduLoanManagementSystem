import { useState } from "react";
import MasterDataTable from "./MasterDataTable";

type Tab = "departments" | "colleges" | "courses";

const TABS: { id: Tab; label: string }[] = [
  { id: "departments", label: "Departments" },
  { id: "colleges", label: "Colleges" },
  { id: "courses", label: "Courses" },
];

export default function AdminMasterDataPage() {
  const [activeTab, setActiveTab] = useState<Tab>("departments");

  return (
    <div className="admin-master-data-page">
      <div className="admin-master-data-card">
        <span className="admin-master-data-accent" />

        <h1>Master Data Management</h1>

        <div className="admin-master-data-tabs" role="tablist">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={activeTab === tab.id}
              className={`admin-master-data-tab ${
                activeTab === tab.id ? "active" : ""
              }`}
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
              {
                key: "standardDurationMonths",
                label: "Duration (months)",
                type: "number",
              },
              { key: "isActive", label: "Active", type: "boolean" },
            ]}
          />
        )}
      </div>
    </div>
  );
}