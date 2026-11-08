// Utilitarios de CPF: mascara (999.999.999-99) e validacao do digito verificador
var FI = window.FI || {};
window.FI = FI;

FI.Cpf = (function ($) {

    function somenteDigitos(valor) {
        return (valor || '').replace(/\D/g, '');
    }

    // Aplica a mascara enquanto o usuario digita (aceita CPF incompleto)
    function mascarar(valor) {
        var d = somenteDigitos(valor).substring(0, 11);

        if (d.length > 9)
            return d.substring(0, 3) + '.' + d.substring(3, 6) + '.' + d.substring(6, 9) + '-' + d.substring(9);
        if (d.length > 6)
            return d.substring(0, 3) + '.' + d.substring(3, 6) + '.' + d.substring(6);
        if (d.length > 3)
            return d.substring(0, 3) + '.' + d.substring(3);

        return d;
    }

    // Formata um CPF completo (11 digitos); se estiver incompleto devolve o valor como veio
    function formatar(valor) {
        return somenteDigitos(valor).length === 11 ? mascarar(valor) : valor;
    }

    // Digito verificador: soma ponderada (pesos n+1 ate 2), resto da divisao por 11
    function calcularDigito(digitos, quantidade) {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += parseInt(digitos.charAt(i), 10) * (quantidade + 1 - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    function validar(valor) {
        if (!/^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/.test($.trim(valor || '')))
            return false;

        var d = somenteDigitos(valor);

        // 000.000.000-00, 111.111.111-11 ... passam no calculo, mas nao sao validos
        if (/^(\d)\1{10}$/.test(d))
            return false;

        return calcularDigito(d, 9) === parseInt(d.charAt(9), 10) &&
               calcularDigito(d, 10) === parseInt(d.charAt(10), 10);
    }

    // Mascara automatica em todo campo marcado com data-mask="cpf"
    $(document).on('input', '[data-mask="cpf"]', function () {
        var mascarado = mascarar(this.value);
        if (this.value !== mascarado)
            this.value = mascarado;
    });

    return {
        somenteDigitos: somenteDigitos,
        mascarar: mascarar,
        formatar: formatar,
        validar: validar
    };
})(jQuery);
