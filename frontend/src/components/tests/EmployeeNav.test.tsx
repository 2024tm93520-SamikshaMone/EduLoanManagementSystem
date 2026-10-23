import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import EmployeeNav from "../pages/EmployeeNav";

describe("EmployeeNav notification navigation", () => {
  it("keeps notification access in the header and removes the sidebar duplicate", () => {
    render(<MemoryRouter><EmployeeNav /></MemoryRouter>);
    expect(screen.getByRole("button", { name: "Notifications" })).toBeInTheDocument();
    expect(screen.queryByText("Notifications")).not.toBeInTheDocument();
  });
});
