import { footballerService } from "../services/footballerService.js";
import { renderTable, renderForm } from "../ui.js";

const container = document.getElementById("app");

export async function showFootballersPage() {
  const data = await footballerService.getAll();
  renderTable(container, data, ["id", "name", "nation", "age", "position", "clubId"]);

  const formDiv = document.createElement("div");
  container.appendChild(formDiv);
  renderForm(formDiv, ["name", "nation", "age", "position", "clubId"], async (newF) => {
    newF.age = parseInt(newF.age);
    newF.clubId = parseInt(newF.clubId);
    await footballerService.create(newF);
    showFootballersPage();
  });
}
