// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
  initializeAllCharacterCounters();
});

function initializeAllCharacterCounters() {
  const textareas = document.querySelectorAll("textarea.character-counted");
  textareas.forEach((textarea) => {
    const maxLength = parseInt(textarea.getAttribute("data-max-length"), 10);
    if (!isNaN(maxLength) && maxLength > 0) {
      // Find the counter display element. It's expected to be a sibling or close by.
      // A more robust way is to use a specific ID or data-attribute for the counter if complex DOM.
      // For now, let's assume it's the next sibling element with class 'char-counter-display'.
      let counterDisplay = textarea.nextElementSibling;
      // If not immediately next, try finding within parent if structure is div > textarea + div.counter
      if (
        !counterDisplay ||
        !counterDisplay.classList.contains("char-counter-display")
      ) {
        // Check if the textarea is wrapped and the counter is a sibling of that wrapper's parent or similar common structures.
        // This simple version assumes it's a direct sibling or the next sibling of its parent if the textarea is wrapped.
        // A more robust solution would be to give the counter a data-for="textarea-id" attribute.
        // For this implementation, we'll assume the .char-counter-display is correctly placed next to the textarea or its direct wrapper.
        if (
          textarea.parentElement &&
          textarea.parentElement.nextElementSibling &&
          textarea.parentElement.nextElementSibling.classList.contains(
            "char-counter-display"
          )
        ) {
          counterDisplay = textarea.parentElement.nextElementSibling;
        } else if (
          textarea.nextElementSibling &&
          textarea.nextElementSibling.classList.contains("char-counter-display")
        ) {
          counterDisplay = textarea.nextElementSibling;
        } else {
          // Fallback: Look for it within the closest form-group or mb-3 parent, then find .char-counter-display
          const parentWrapper =
            textarea.closest(".form-group") || textarea.closest(".mb-3");
          if (parentWrapper) {
            counterDisplay = parentWrapper.querySelector(
              ".char-counter-display"
            );
          }
        }
      }

      if (counterDisplay) {
        const updateCounter = () => {
          const currentLength = textarea.value.length;
          if (currentLength > maxLength) {
            textarea.value = textarea.value.substring(0, maxLength);
          }
          // Re-evaluate length after potential truncation
          const finalLength = textarea.value.length;
          counterDisplay.textContent = `${finalLength} / ${maxLength}`;
          if (finalLength >= maxLength) {
            counterDisplay.classList.add("text-danger");
            counterDisplay.classList.remove("text-muted");
          } else {
            counterDisplay.classList.remove("text-danger");
            counterDisplay.classList.add("text-muted");
          }
        };

        textarea.addEventListener("input", updateCounter);
        textarea.addEventListener("keyup", updateCounter); // For immediate feedback on cut/paste
        updateCounter(); // Initial update
      } else {
        console.warn("Char counter display not found for textarea:", textarea);
      }
    }
  });
}

// If you have dynamically added forms (e.g., via AJAX), you might need to call
// initializeAllCharacterCounters() again after the new content is loaded.
// For comment edit forms that are initially hidden and then shown, this DOMContentLoaded
// approach should work as they are part of the initial DOM.
