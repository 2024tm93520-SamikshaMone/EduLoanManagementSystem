import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import EligibilityRulesTable from "./EligibilityRulesTable";
import * as ruleService from "../../services/eligibilityRuleService";

const SAMPLE_RULES: ruleService.EligibilityRule[] = [
  {
    id: 1, ruleName: "Minimum Tenure", ruleCategory: "Tenure", conditionField: "TenureMonths",
    operator: ">=", conditionValue: 12, errorMessage: "Too new.", severity: "Blocking", isActive: true,
  },
];

describe("EligibilityRulesTable", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("shows a loading state, then renders the rule list", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue(SAMPLE_RULES);
    render(<EligibilityRulesTable />);

    expect(screen.getByRole("status")).toHaveTextContent(/loading/i);

    expect(await screen.findByText("Minimum Tenure")).toBeInTheDocument();
    expect(screen.getByText("Blocking")).toBeInTheDocument();
    expect(screen.getByText(/TenureMonths >= 12/)).toBeInTheDocument();
  });

  it("shows an empty state when there are no rules", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue([]);
    render(<EligibilityRulesTable />);

    expect(await screen.findByText(/no eligibility rules yet/i)).toBeInTheDocument();
  });

  it("shows a load error if fetching fails", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockRejectedValue(
      new ruleService.EligibilityRuleError("Could not load rules.")
    );
    render(<EligibilityRulesTable />);

    expect(await screen.findByText(/could not load rules/i)).toBeInTheDocument();
  });

  it("adds a new rule through the Add form", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue(SAMPLE_RULES);
    const createSpy = vi.spyOn(ruleService, "createEligibilityRule").mockResolvedValue({
      id: 2, ruleName: "Min Salary", ruleCategory: "Salary", conditionField: "MonthlySalary",
      operator: ">=", conditionValue: 20000, errorMessage: "Too low.", severity: "Blocking", isActive: true,
    });

    const user = userEvent.setup();
    render(<EligibilityRulesTable />);
    await screen.findByText("Minimum Tenure");

    await user.click(screen.getByRole("button", { name: /add rule/i }));
    await user.type(screen.getByLabelText(/rule name/i), "Min Salary");
    await user.type(screen.getByLabelText(/category/i), "Salary");
    await user.type(screen.getByLabelText(/error message/i), "Too low.");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(createSpy).toHaveBeenCalledWith(expect.objectContaining({ ruleName: "Min Salary" }));
    });
  });

  it("shows a validation error when rule name is empty on submit", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue(SAMPLE_RULES);
    const createSpy = vi.spyOn(ruleService, "createEligibilityRule");

    const user = userEvent.setup();
    render(<EligibilityRulesTable />);
    await screen.findByText("Minimum Tenure");

    await user.click(screen.getByRole("button", { name: /add rule/i }));
    await user.click(screen.getByRole("button", { name: /save/i }));

    expect(await screen.findByText(/rule name is required/i)).toBeInTheDocument();
    expect(createSpy).not.toHaveBeenCalled();
  });

  it("edits an existing rule, pre-filling the form", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue(SAMPLE_RULES);
    const updateSpy = vi.spyOn(ruleService, "updateEligibilityRule").mockResolvedValue({
      ...SAMPLE_RULES[0], conditionValue: 18,
    });

    const user = userEvent.setup();
    render(<EligibilityRulesTable />);
    await screen.findByText("Minimum Tenure");

    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    const valueField = screen.getByLabelText(/^value$/i) as HTMLInputElement;
    expect(valueField.value).toBe("12");

    await user.clear(valueField);
    await user.type(valueField, "18");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(updateSpy).toHaveBeenCalledWith(1, expect.objectContaining({ conditionValue: 18 }));
    });
  });

  it("deletes a rule and shows an error if rejected (e.g. already used)", async () => {
    vi.spyOn(ruleService, "getEligibilityRules").mockResolvedValue(SAMPLE_RULES);
    vi.spyOn(ruleService, "deleteEligibilityRule").mockRejectedValue(
      new ruleService.EligibilityRuleError("This rule has already been used to evaluate applications and cannot be deleted.")
    );

    const user = userEvent.setup();
    render(<EligibilityRulesTable />);
    await screen.findByText("Minimum Tenure");

    await user.click(screen.getByRole("button", { name: /^delete$/i }));

    expect(await screen.findByText(/cannot be deleted/i)).toBeInTheDocument();
  });
});
