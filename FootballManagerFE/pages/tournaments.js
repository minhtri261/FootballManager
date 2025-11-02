import { tournamentService } from "../services/tournamentService.js";
import { renderTable, renderForm } from "../ui.js";

const container = document.getElementById("app");

export async function showTournamentsPage() {
  const data = await tournamentService.getAll();
  renderTable(container, data, ["id", "name", "seasonNumber", "teamsCount", "type"]);

  const formDiv = document.createElement("div");
  container.appendChild(formDiv);
  renderForm(formDiv, ["name", "seasonNumber", "teamsCount", "type"], async (newT) => {
    newT.seasonNumber = parseInt(newT.seasonNumber);
    newT.teamsCount = parseInt(newT.teamsCount);
    await tournamentService.create(newT);
    showTournamentsPage();
  });
}
