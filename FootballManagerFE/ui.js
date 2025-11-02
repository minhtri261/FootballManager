export function renderTable(container, data, fields) {
  container.innerHTML = "";
  const table = document.createElement("table");
  table.className = "data-table";
  const thead = document.createElement("thead");
  thead.innerHTML = `<tr>${fields.map(f => `<th>${f}</th>`).join("")}<th>Actions</th></tr>`;
  table.appendChild(thead);

  const tbody = document.createElement("tbody");
  data.forEach(item => {
    const row = document.createElement("tr");
    row.innerHTML = fields.map(f => `<td>${item[f]}</td>`).join("") +
      `<td>
        <button class="edit-btn" data-id="${item.id}">Edit</button>
        <button class="del-btn" data-id="${item.id}">Delete</button>
      </td>`;
    tbody.appendChild(row);
  });
  table.appendChild(tbody);
  container.appendChild(table);
}

export function renderForm(container, fields, onSubmit) {
  container.innerHTML = "";
  const form = document.createElement("form");
  form.className = "data-form";

  fields.forEach(f => {
    const label = document.createElement("label");
    label.textContent = f;
    const input = document.createElement("input");
    input.name = f;
    label.appendChild(input);
    form.appendChild(label);
  });

  const btn = document.createElement("button");
  btn.type = "submit";
  btn.textContent = "Save";
  form.appendChild(btn);

  form.onsubmit = e => {
    e.preventDefault();
    const data = {};
    fields.forEach(f => data[f] = form[f].value);
    onSubmit(data);
  };

  container.appendChild(form);
}
