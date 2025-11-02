import { showClubsPage } from "./pages/clubs.js";
import { showFootballersPage } from "./pages/footballers.js";
import { showTournamentsPage } from "./pages/tournaments.js";

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("nav-clubs").onclick = showClubsPage;
  document.getElementById("nav-footballers").onclick = showFootballersPage;
  document.getElementById("nav-tournaments").onclick = showTournamentsPage;

  showClubsPage(); // Mặc định vào trang CLB
});
