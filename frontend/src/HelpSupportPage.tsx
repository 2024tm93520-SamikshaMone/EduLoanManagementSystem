import { useMemo, useState } from "react";
import "./index.css";

type HelpCard = {
  title: string;
  description: string;
  icon: string;
};

type Faq = {
  question: string;
  answer: string;
};

export default function HelpSupportPage() {
  const [search, setSearch] = useState("");
  const [openFaq, setOpenFaq] = useState<number | null>(null);

  /* =========================================================
     DETECT CURRENT PORTAL
     ========================================================= */

  const role = useMemo(() => {
    const path = window.location.pathname.toLowerCase();

    if (path.startsWith("/admin")) return "Admin";
    if (path.startsWith("/hr")) return "HR";
    if (path.startsWith("/finance")) return "Finance";

    return "Employee";
  }, []);

  /* =========================================================
     ROLE BASED CONTENT
     ========================================================= */

  const roleContent: Record<
    string,
    {
      subtitle: string;
      quickHelp: HelpCard[];
      faqs: Faq[];
    }
  > = {
    /* =======================================================
       EMPLOYEE
       ======================================================= */

    Employee: {
      subtitle:
        "Find guidance for your profile, education loan application and application status.",

      quickHelp: [
        {
          title: "My Profile",
          description:
            "View your employee information, department, designation and contact details.",
          icon: "♙",
        },
        {
          title: "Loan Application",
          description:
            "Create and submit an education loan application with the required details.",
          icon: "?",
        },
        {
          title: "Documents",
          description:
            "Upload and manage the documents required for your education loan application.",
          icon: "▣",
        },
        {
          title: "Application Status",
          description:
            "Track your application and view its current review or approval status.",
          icon: "↻",
        },
        {
          title: "Eligibility",
          description:
            "Understand the eligibility checks performed for your education loan.",
          icon: "✓",
        },
        {
          title: "Password & Account",
          description:
            "Get help with password changes, password reset and account access.",
          icon: "🔑",
        },
      ],

      faqs: [
        {
          question: "How can I update my profile information?",
          answer:
            "Open My Profile from the employee navigation. You can view your employee and employment information and update the details that are allowed by the portal.",
        },
        {
          question: "How can I apply for an education loan?",
          answer:
            "Open New Application, provide your education and loan details, upload the required documents and submit the application for processing.",
        },
        {
          question: "What documents are required for my application?",
          answer:
            "The required documents depend on the education loan application requirements. Upload the applicable supporting documents from the Documents section of your application.",
        },
        {
          question: "Where can I check my application status?",
          answer:
            "Open My Applications to view your submitted applications and their current status.",
        },
        {
          question: "What happens after I submit my application?",
          answer:
            "The application is reviewed according to the configured eligibility and validation process. Additional information may be requested when required.",
        },
        {
          question: "How is loan eligibility checked?",
          answer:
            "Your application is evaluated against the configured eligibility rules and validation requirements before it proceeds through the approval workflow.",
        },
        {
          question: "How can I reset my password?",
          answer:
            "Use the Forgot Password option on the login page to reset your password.",
        },
        {
          question: "How can I contact support?",
          answer:
            "Use the Contact Support section at the bottom of this page to contact the support team.",
        },
      ],
    },

    /* =======================================================
       ADMIN
       ======================================================= */

    Admin: {
      subtitle:
        "Find guidance for employee management, master data, eligibility rules, applications and reports.",

      quickHelp: [
        {
          title: "Employee Management",
          description:
            "View employee information and manage employee-related details.",
          icon: "♙",
        },
        {
          title: "Loan Applications",
          description:
            "Review employee education loan applications and application details.",
          icon: "?",
        },
        {
          title: "Master Data",
          description:
            "Manage departments, colleges and courses used by the portal.",
          icon: "▣",
        },
        {
          title: "Eligibility Rules",
          description:
            "Configure and maintain rules used for employee loan eligibility.",
          icon: "✓",
        },
        {
          title: "Reports",
          description:
            "View application, department, course and college-wise reports.",
          icon: "▤",
        },
        {
          title: "Application Workflow",
          description:
            "Understand the application review, approval and rejection flow.",
          icon: "↻",
        },
      ],

      faqs: [
        {
          question: "How can I manage employees?",
          answer:
            "Open Employees from the admin navigation to view employee information and available employee management options.",
        },
        {
          question: "Where can I manage departments, colleges and courses?",
          answer:
            "Open Master Data. You can manage the master data used throughout the education loan application process.",
        },
        {
          question: "Where can I manage eligibility rules?",
          answer:
            "Open Eligibility Rules to view and maintain the configured eligibility rules used by the system.",
        },
        {
          question: "Where can I review loan applications?",
          answer:
            "Open Loan Applications from the admin navigation to review submitted employee applications and their details.",
        },
        {
          question: "Where can I view reports?",
          answer:
            "Open Reports to view application and education-related information based on the available report filters.",
        },
        {
          question: "What is the application workflow?",
          answer:
            "The application workflow supports review, validation, approval, rejection and request-for-information scenarios.",
        },
        {
          question: "How can I contact support?",
          answer:
            "Use the Contact Support section at the bottom of this page to contact the support team.",
        },
      ],
    },

    /* =======================================================
       HR
       ======================================================= */

    HR: {
      subtitle:
        "Find guidance for employee information, application review, workflow and education insights.",

      quickHelp: [
        {
          title: "Employee Information",
          description:
            "View employee information relevant to the education loan process.",
          icon: "♙",
        },
        {
          title: "Loan Applications",
          description:
            "Review employee education loan applications assigned for HR processing.",
          icon: "?",
        },
        {
          title: "Application Review",
          description:
            "Review submitted information and supporting documents during the workflow.",
          icon: "▣",
        },
        {
          title: "Eligibility",
          description:
            "Review the eligibility results generated from the configured rules.",
          icon: "✓",
        },
        {
          title: "Reports",
          description:
            "View application and employee education insights available to HR.",
          icon: "▤",
        },
        {
          title: "Workflow",
          description:
            "Understand review, request-for-information, approval and rejection stages.",
          icon: "↻",
        },
      ],

      faqs: [
        {
          question: "Where can I review employee loan applications?",
          answer:
            "Open Loan Applications to review the applications available to HR and inspect their submitted information.",
        },
        {
          question: "What information can HR review?",
          answer:
            "HR can review employee, education, loan and supporting document information available within the application workflow.",
        },
        {
          question: "Where can I view eligibility results?",
          answer:
            "Open the relevant loan application to review the eligibility checks and their results.",
        },
        {
          question: "Can an application be sent back for more information?",
          answer:
            "The workflow supports requesting additional information when the submitted application requires clarification or missing details.",
        },
        {
          question: "Where can I view education insights?",
          answer:
            "Use the available Reports section to review employee education and application-related insights.",
        },
        {
          question: "How can I contact support?",
          answer:
            "Use the Contact Support section at the bottom of this page to contact the support team.",
        },
      ],
    },

    /* =======================================================
       FINANCE
       ======================================================= */

    Finance: {
      subtitle:
        "Find guidance for approved loan applications, processing and confirmation.",

      quickHelp: [
        {
          title: "Loan Applications",
          description:
            "Review applications that have reached the finance processing stage.",
          icon: "?",
        },
        {
          title: "Application Details",
          description:
            "Review employee, education and approved loan information.",
          icon: "▣",
        },
        {
          title: "Processing",
          description:
            "Review applications requiring finance processing after approval.",
          icon: "↻",
        },
        {
          title: "Processing Confirmation",
          description:
            "Record or review confirmation after the loan amount has been processed.",
          icon: "✓",
        },
        {
          title: "Reports",
          description:
            "View loan application and processing-related reports.",
          icon: "▤",
        },
        {
          title: "Workflow",
          description:
            "Understand the handoff from approval to finance processing and confirmation.",
          icon: "→",
        },
      ],

      faqs: [
        {
          question: "Which applications are handled by Finance?",
          answer:
            "Finance handles applications that have reached the finance processing stage after the required approval workflow.",
        },
        {
          question: "What information should Finance review?",
          answer:
            "Finance can review the employee, education and approved loan information required for further processing.",
        },
        {
          question: "Does the portal perform the actual loan disbursement?",
          answer:
            "No. The system supports the workflow up to processing confirmation. Actual loan disbursement is handled outside the proposed platform.",
        },
        {
          question: "Where can I provide processing confirmation?",
          answer:
            "Open the relevant application and use the available finance processing or confirmation functionality.",
        },
        {
          question: "Where can I view finance-related reports?",
          answer:
            "Open Reports to view the reporting information available for loan applications and processing.",
        },
        {
          question: "How can I contact support?",
          answer:
            "Use the Contact Support section at the bottom of this page to contact the support team.",
        },
      ],
    },
  };

  const content = roleContent[role];

  /* =========================================================
     SEARCH
     ========================================================= */

  const filteredCards = content.quickHelp.filter(
    (card) =>
      card.title.toLowerCase().includes(search.toLowerCase()) ||
      card.description.toLowerCase().includes(search.toLowerCase())
  );

  const filteredFaqs = content.faqs.filter(
    (faq) =>
      faq.question.toLowerCase().includes(search.toLowerCase()) ||
      faq.answer.toLowerCase().includes(search.toLowerCase())
  );

  /* =========================================================
     FAQ TOGGLE
     ========================================================= */

  const toggleFaq = (index: number) => {
    setOpenFaq(openFaq === index ? null : index);
  };

  /* =========================================================
     QUICK HELP SCROLL
     ========================================================= */

  const scrollTo = (keyword: string) => {
    const faqSection = document.getElementById("help-faq");

    if (!faqSection) return;

    const faqIndex = content.faqs.findIndex(
      (faq) =>
        faq.question.toLowerCase().includes(keyword.toLowerCase()) ||
        faq.answer.toLowerCase().includes(keyword.toLowerCase())
    );

    faqSection.scrollIntoView({
      behavior: "smooth",
      block: "start",
    });

    if (faqIndex >= 0) {
      setOpenFaq(faqIndex);
    }
  };

  /* =========================================================
     QUICK HELP KEYWORD MAPPING
     ========================================================= */

  const handleQuickHelpClick = (title: string) => {
    const titleLower = title.toLowerCase();

    if (
      titleLower.includes("profile") ||
      titleLower.includes("employee information") ||
      titleLower.includes("employee management")
    ) {
      scrollTo("profile");
      return;
    }

    if (
      titleLower.includes("loan application") ||
      titleLower.includes("loan applications")
    ) {
      scrollTo("application");
      return;
    }

    if (titleLower.includes("document")) {
      scrollTo("documents");
      return;
    }

    if (
      titleLower.includes("eligibility") ||
      titleLower.includes("eligibility rules")
    ) {
      scrollTo("eligibility");
      return;
    }

    if (
      titleLower.includes("status") ||
      titleLower.includes("processing confirmation")
    ) {
      scrollTo("status");
      return;
    }

    if (
      titleLower.includes("report") ||
      titleLower.includes("reports")
    ) {
      scrollTo("reports");
      return;
    }

    if (
      titleLower.includes("workflow") ||
      titleLower.includes("application review") ||
      titleLower.includes("processing")
    ) {
      scrollTo("workflow");
      return;
    }

    if (
      titleLower.includes("password") ||
      titleLower.includes("account")
    ) {
      scrollTo("password");
      return;
    }

    scrollTo("support");
  };

  /* =========================================================
     UI
     ========================================================= */

  return (
    <div className="help-support-page">
      <div className="help-support-container">

        {/* ===================================================
            HEADER
           =================================================== */}

        <div className="help-support-header">
          <div>
            <h1>Help & Support</h1>

            <p>{content.subtitle}</p>
          </div>

          <div className="help-support-role-badge">
            {role} Portal
          </div>
        </div>

        {/* ===================================================
            SEARCH
           =================================================== */}

        <section className="help-support-search-card">
          <div className="help-support-search-content">

            <div>
              <h2>How can we help?</h2>

              <p>
                Search for guidance related to your portal activities.
              </p>
            </div>

            <div className="help-support-search">
              <span className="help-support-search-icon">
                ⌕
              </span>

              <input
                type="text"
                placeholder="Search help topics..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setOpenFaq(null);
                }}
              />
            </div>

          </div>
        </section>

        {/* ===================================================
            QUICK HELP
           =================================================== */}

        <section className="help-support-section">

          <div className="help-support-section-header">
            <h2>Quick Help</h2>

            <p>
              Find guidance for common portal activities.
            </p>
          </div>

          {filteredCards.length > 0 ? (
            <div className="help-support-quick-grid">

              {filteredCards.map((card) => (
                <button
                  key={card.title}
                  type="button"
                  className="help-support-quick-card"
                  onClick={() =>
                    handleQuickHelpClick(card.title)
                  }
                >
                  <div className="help-support-quick-icon">
                    {card.icon}
                  </div>

                  <div>
                    <h3>{card.title}</h3>

                    <p>{card.description}</p>
                  </div>
                </button>
              ))}

            </div>
          ) : (
            <div className="help-support-empty">
              No help topics found for "{search}".
            </div>
          )}

        </section>

        {/* ===================================================
            FAQ
           =================================================== */}

        <section
          id="help-faq"
          className="help-support-section"
        >

          <div className="help-support-section-header">
            <h2>Frequently Asked Questions</h2>

            <p>
              Find answers to common questions.
            </p>
          </div>

          <div className="help-support-faq-card">

            {filteredFaqs.length > 0 ? (
              filteredFaqs.map((faq, index) => (

                <div
                  className="help-support-faq-item"
                  key={faq.question}
                >

                  <button
                    type="button"
                    className="help-support-faq-question"
                    onClick={() => toggleFaq(index)}
                  >

                    <span>{faq.question}</span>

                    <span
                      className={`help-support-faq-chevron ${
                        openFaq === index ? "open" : ""
                      }`}
                    >
                      ⌄
                    </span>

                  </button>

                  {openFaq === index && (
                    <div className="help-support-faq-answer">
                      {faq.answer}
                    </div>
                  )}

                </div>

              ))
            ) : (
              <div className="help-support-empty">
                No frequently asked questions found.
              </div>
            )}

          </div>

        </section>

        {/* ===================================================
            CONTACT SUPPORT
           =================================================== */}

        <section
          id="help-support"
          className="help-support-section"
        >

          <div className="help-support-contact-card">

            <div className="help-support-contact-icon">
              ?
            </div>

            <div className="help-support-contact-content">

              <h2>Still need help?</h2>

              <p>
                If you cannot find the information you need,
                contact the support team for assistance.
              </p>

              <div className="help-support-contact-details">

                <div>
                  <span>Email</span>

                  <strong>
                    eduloan.support@company.com
                  </strong>
                </div>

                <div>
                  <span>Support Hours</span>

                  <strong>
                    Monday - Friday, 9:00 AM - 6:00 PM
                  </strong>
                </div>

              </div>

            </div>

            <a
              className="help-support-contact-button"
              href="mailto:eduloan.support@company.com"
            >
              Contact Support
            </a>

          </div>

        </section>

      </div>
    </div>
  );
}