function AddLikeOrDislike(reviewId, action) {
    fetch('/Reaction/AddReactionToReview', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reviewId: reviewId, action: action })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const countElementId = action === "like" ? `like-count-${reviewId}` : `dislike-count-${reviewId}`;
                const countElement = document.getElementById(countElementId);
                const currentCount = parseInt(countElement.textContent, 10);
                countElement.textContent = currentCount + 1;
                location.reload();
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                }
            } else {
                alert(data.message || 'An error occurred.');
            }
        })
        .catch(error => {
            alert('An error occurred.');
        });
}

document.addEventListener("DOMContentLoaded", function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/commentsHub")
        .build();

    connection.on("NewComment", function (data) {
        const newComment = document.createElement('div');
        newComment.className = 'card mb-3';

        const header = document.createElement('div');
        header.className = 'card-header';
        header.innerHTML = `<h2>${data.author}</h2>`;
        newComment.appendChild(header);

        const body = document.createElement('div');
        body.className = 'card-body';
        body.innerHTML = `<p>${data.text}</p>`;
        newComment.appendChild(body);
        const commentForm = document.querySelector(`.comment-form[data-review-id="${data.reviewId}"]`);
        if (commentForm) {
            const commentsContainer = commentForm.closest('.card.mb-3').querySelector('.comments-container');
            commentsContainer.appendChild(newComment);
        }
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

    const likeButtons = document.querySelectorAll('.like-dislike-btn');
    likeButtons.forEach(button => {
        button.addEventListener('click', function (event) {
            event.preventDefault();
            const reviewId = event.currentTarget.getAttribute('data-id');
            const action = event.currentTarget.getAttribute('data-action');
            AddLikeOrDislike(reviewId, action);
        });
    });

    const commentForms = document.querySelectorAll('.comment-form');
    commentForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(form);
            fetch(window.location.origin + '/Comment/Add', {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    if (!data.success) {
                        alert('Error adding comment.');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Error adding comment.');
                });
        });
    });
    let forms = document.querySelectorAll(".delete-review-form");
    forms.forEach(form => {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            let reviewId = this.querySelector(".deleteButton").getAttribute("data-review-id");

            fetch('/Review/DeleteReview?ReviewId=' + reviewId, {
                method: 'DELETE',
            })
                .then(response => {
                    if (response.ok) {
                        window.location.href = "/Home/Index";
                    } else {
                        console.error('Failed to delete review');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                });
        });
    });
});