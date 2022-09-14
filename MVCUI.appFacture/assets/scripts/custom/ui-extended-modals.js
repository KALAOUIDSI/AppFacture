var UIExtendedModals = function () {

    
    return {
        //main function to initiate the module
        init: function () {
        
            // general settings
            $.fn.modal.defaults.spinner = $.fn.modalmanager.defaults.spinner = 
              '<div class="loading-spinner" style="width: 200px; margin-left: -100px;">' +
                '<div class="progress progress-striped active">' +
                  '<div class="progress-bar" style="width: 100%;"></div>' +
                '</div>' +
              '</div>';

            $.fn.modalmanager.defaults.resize = true;

            //ajax 
            var $modal = $('#ajax-modal');

            $('.view').on('click', function (e) {
                e.preventDefault();
              // create the backdrop and wait for next modal to be triggered
              $('body').modalmanager('loading');
              var href = $(this).attr('href');
              setTimeout(function () {
                  $modal.load(href, '', function () {
                  $modal.modal();
                });
              }, 1000);
            });

            
        }

    };

}();