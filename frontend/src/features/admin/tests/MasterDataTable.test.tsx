import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import MasterDataTable from "./MasterDataTable";
import * as masterDataService from "../../services/masterDataService";

const DEPARTMENT_COLUMNS = [
  { key: "name", label: "Name", type: "text" as const },
  { key: "isActive", label: "Active", type: "boolean" as const },
];

const SAMPLE_DEPARTMENTS = [
  { id: 1, name: "Engineering", isActive: true },
  { id: 2, name: "Finance", isActive: true },
];

describe("MasterDataTable", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("shows a loading state, then renders the list", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue(SAMPLE_DEPARTMENTS);
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    expect(screen.getByRole("status")).toHaveTextContent(/loading/i);

    expect(await screen.findByText("Engineering")).toBeInTheDocument();
    expect(screen.getByText("Finance")).toBeInTheDocument();
  });

  it("shows an empty state when there are no items", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue([]);
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    expect(await screen.findByText(/no departments yet/i)).toBeInTheDocument();
  });

  it("shows a load error if fetching fails", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockRejectedValue(
      new masterDataService.MasterDataError("Could not load data.")
    );
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    expect(await screen.findByText(/could not load data/i)).toBeInTheDocument();
  });

  it("adds a new item through the Add form", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue(SAMPLE_DEPARTMENTS);
    const createSpy = vi
      .spyOn(masterDataService, "createMasterDataItem")
      .mockResolvedValue({ id: 3, name: "HR", isActive: true });

    const user = userEvent.setup();
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    await screen.findByText("Engineering");
    await user.click(screen.getByRole("button", { name: /add department/i }));

    await user.type(screen.getByLabelText(/name/i), "HR");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(createSpy).toHaveBeenCalledWith("departments", expect.objectContaining({ name: "HR" }));
    });
  });

  it("shows a validation error when the required text field is empty on submit", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue(SAMPLE_DEPARTMENTS);
    const createSpy = vi.spyOn(masterDataService, "createMasterDataItem");

    const user = userEvent.setup();
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    await screen.findByText("Engineering");
    await user.click(screen.getByRole("button", { name: /add department/i }));
    await user.click(screen.getByRole("button", { name: /save/i }));

    expect(await screen.findByText(/name is required/i)).toBeInTheDocument();
    expect(createSpy).not.toHaveBeenCalled();
  });

  it("edits an existing item, pre-filling the form with its current values", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue(SAMPLE_DEPARTMENTS);
    const updateSpy = vi
      .spyOn(masterDataService, "updateMasterDataItem")
      .mockResolvedValue({ id: 1, name: "Engineering & R&D", isActive: true });

    const user = userEvent.setup();
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    await screen.findByText("Engineering");
    const editButtons = screen.getAllByRole("button", { name: /^edit$/i });
    await user.click(editButtons[0]);

    const nameField = screen.getByLabelText(/name/i) as HTMLInputElement;
    expect(nameField.value).toBe("Engineering");

    await user.clear(nameField);
    await user.type(nameField, "Engineering & R&D");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(updateSpy).toHaveBeenCalledWith(
        "departments",
        1,
        expect.objectContaining({ name: "Engineering & R&D" })
      );
    });
  });

  it("deletes an item and shows an error if the delete is rejected (e.g. in-use)", async () => {
    vi.spyOn(masterDataService, "getMasterDataList").mockResolvedValue(SAMPLE_DEPARTMENTS);
    vi.spyOn(masterDataService, "deleteMasterDataItem").mockRejectedValue(
      new masterDataService.MasterDataError("This department has employees assigned to it and cannot be deleted.")
    );

    const user = userEvent.setup();
    render(<MasterDataTable title="Departments" entity="departments" columns={DEPARTMENT_COLUMNS} />);

    await screen.findByText("Engineering");
    const deleteButtons = screen.getAllByRole("button", { name: /^delete$/i });
    await user.click(deleteButtons[0]);

    expect(await screen.findByText(/cannot be deleted/i)).toBeInTheDocument();
  });
});
