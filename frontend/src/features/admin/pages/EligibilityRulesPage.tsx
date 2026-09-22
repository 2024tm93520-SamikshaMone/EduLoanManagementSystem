import EligibilityRulesTable from "./EligibilityRulesTable";
import "../styles/AdminMasterDataPage.css";

export default function EligibilityRulesPage() {
  return (
    <div className="admin-master-data-page">
      <div className="admin-master-data-card">
        <span className="admin-master-data-accent" />

        <h1>Eligibility Rules</h1>

        <EligibilityRulesTable />
      </div>
    </div>
  );
}