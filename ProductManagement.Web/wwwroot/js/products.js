// Product List page: Add/Edit in a modal form loaded by AJAX, and Delete confirmed in a modal.
// The server answers each AJAX request with a status code (see ProductsController):
//   200 = table rows, 422 = form with validation messages, 404 = product gone, other = failure.
$(function () {
    'use strict';

    const $page = $('#products-page');
    const $rows = $('#product-rows');
    const $alerts = $('#product-alerts');
    const $productModal = $('#product-modal');
    const $deleteModal = $('#delete-modal');
    const productModal = bootstrap.Modal.getOrCreateInstance($productModal[0]);
    const deleteModal = bootstrap.Modal.getOrCreateInstance($deleteModal[0]);

    const notFoundMessage = 'That product no longer exists. The list has been refreshed.';
    const failureMessage = 'Something went wrong. Please try again.';

    // Shows a dismissible message above the table. Text is set with .text() so it's never parsed as HTML.
    function showAlert(message, type) {
        const $alert = $('<div class="alert alert-dismissible fade show" role="alert"></div>')
            .addClass('alert-' + type)
            .text(message)
            .append('<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>');
        $alerts.empty().append($alert);
    }

    function showModalError($container, message) {
        $container.find('.js-modal-error').text(message).removeClass('d-none');
    }

    function refreshRows() {
        return $.get($page.data('rows-url')).done(function (html) {
            $rows.html(html);
        });
    }

    // Puts a form partial into the modal. jQuery Validation only scans the page once, at load,
    // so a form added later has to be parsed explicitly or client-side validation silently stops.
    function setForm(html) {
        $productModal.find('.modal-content').html(html);
        const $form = $productModal.find('form');
        $form.removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse($form);
    }

    function handleNotFound(modal) {
        modal.hide();
        showAlert(notFoundMessage, 'warning');
        refreshRows();
    }

    function openForm(url) {
        $.get(url)
            .done(function (html) {
                setForm(html);
                productModal.show();
            })
            .fail(function (xhr) {
                if (xhr.status === 404) {
                    handleNotFound(productModal);
                } else {
                    showAlert(failureMessage, 'danger');
                }
            });
    }

    $('#add-product').on('click', function () {
        openForm($page.data('create-url'));
    });

    // Delegated handlers, because the rows are replaced after every change.
    $rows.on('click', '.js-edit', function () {
        openForm($(this).data('url'));
    });

    $rows.on('click', '.js-delete', function () {
        const $button = $(this);
        $deleteModal.find('.js-delete-name').text($button.data('name'));
        $deleteModal.find('.js-modal-error').addClass('d-none');
        $deleteModal.data('url', $button.data('url'));
        deleteModal.show();
    });

    // Add/Edit submit. Delegated, because the form is replaced each time it's loaded.
    $productModal.on('submit', 'form', function (event) {
        event.preventDefault();
        const $form = $(this);
        const $save = $form.find('[type="submit"]');

        // Ignore the submit if the form is invalid, or if a save is already in flight (double click).
        if (!$form.valid() || $save.prop('disabled')) {
            return;
        }
        $save.prop('disabled', true);

        const isEdit = $form.data('mode') === 'edit';

        // serialize() includes the anti-forgery token the form tag helper rendered.
        $.post($form.attr('action'), $form.serialize())
            .done(function (html) {
                $rows.html(html);
                productModal.hide();
                showAlert(isEdit ? 'Product updated.' : 'Product added.', 'success');
            })
            .fail(function (xhr) {
                if (xhr.status === 422) {
                    setForm(xhr.responseText); // the new form comes with its Save button enabled
                } else if (xhr.status === 404) {
                    handleNotFound(productModal);
                } else {
                    showModalError($form, failureMessage);
                }
            })
            .always(function () {
                $save.prop('disabled', false);
            });
    });

    // Delete confirm. Uses the form's submit event so Enter works as well as the button.
    $('#delete-form').on('submit', function (event) {
        event.preventDefault();
        const $form = $(this);
        const $confirm = $form.find('[type="submit"]');
        if ($confirm.prop('disabled')) {
            return;
        }
        $confirm.prop('disabled', true);

        $.post($deleteModal.data('url'), $form.serialize())
            .done(function (html) {
                $rows.html(html);
                deleteModal.hide();
                showAlert('Product deleted.', 'success');
            })
            .fail(function (xhr) {
                if (xhr.status === 404) {
                    handleNotFound(deleteModal);
                } else {
                    showModalError($form, failureMessage);
                }
            })
            .always(function () {
                $confirm.prop('disabled', false);
            });
    });
});
