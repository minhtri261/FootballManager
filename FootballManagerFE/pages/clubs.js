import { clubService } from "../services/clubService.js";
import { renderTable, renderForm } from "../ui.js";

const container = document.getElementById("app");

export async function showClubsPage() {
  const data = await clubService.getAll();
  renderTable(container, data, ["id", "name", "nation", "money"]);

  const formDiv = document.createElement("div");
  container.appendChild(formDiv);
  renderForm(formDiv, ["name", "nation", "money"], async (newClub) => {
    newClub.money = parseFloat(newClub.money);
    await clubService.create(newClub);
    showClubsPage();
  });
}
