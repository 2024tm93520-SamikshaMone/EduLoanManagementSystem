import { useNavigate } from "react-router-dom";
import NewApplicationWizard from "././NewApplicationWizard";
import "../styles/NewApplicationPage.css";


export default function NewApplicationPage() {
  const navigate = useNavigate();

  function handleWizardDone() {
    navigate("/employee/applications");
  }

  return (
    <div className="admin-page">
      <div className="admin-card">
        <h1>New Loan Application</h1>

        <NewApplicationWizard onDone={handleWizardDone} />

        <button
          type="button"
          className="link-button"
          onClick={() => navigate("/employee/applications")}
        >
          Cancel and go back to list
        </button>
      </div>
    </div>
  );
}