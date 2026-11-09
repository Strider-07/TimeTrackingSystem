function changeTheme() {
    console.log("Elo");
    const html = document.documentElement;
    const currentTheme = html.getAttribute("data-theme");
    const newTheme = currentTheme === "light" ? "dark" : "light";
    html.setAttribute("data-theme", newTheme);
    localStorage.setItem("theme", newTheme);
}

let savedTheme = localStorage.getAttribute("theme");
if (savedTheme) {
    document.documentElement.setAttribute("data-theme", savedTheme);
}