// Wait for the DOM to be ready before running the script
$(document).ready(function () {
    // Event listener for "Add to Cart" buttons
    $(".add_to_cart .btn").on("click", function () {
        // Get the product ID from the data attribute
        var productId = $(this).data("product-id");

        // Call the updateCart function with productId and count (1 in this case)
        updateCart(productId, 1);
    });
});

function updateCart(productId, count) {
        $.ajax({
            url: '/Cart/UpdateCart',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ productId: productId, count: count }),
            success: function (result) {
                $(".item_count").html(result);
                if (count == 0) {
                    $("#" + productId).remove();
                }
                getMiniCart();
            },
            error: function () {
                alert('Error product not found or QTY is not enough!');
            }
        });
        }

    function getMiniCart() {
        $.ajax({
            url: '/Cart/SmallCart',
            type: 'GET',
            success: function (result) {
                $(".mini_cart").html(result);
            },
            error: function () {
                alert('Error fetching mini cart');
            }
        });
        }

   document.addEventListener("DOMContentLoaded", function () {
    getMiniCart();
    document.querySelectorAll(".slick-slide").forEach(function (slide) {
        slide.addEventListener("click", function () {
            document.querySelector(".slick-track").style.transform = "";
        });
    });
});


