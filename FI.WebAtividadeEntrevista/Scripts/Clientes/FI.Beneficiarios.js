// Pop-up de beneficiarios do cliente.
// A lista fica em memoria e so e enviada ao servidor (e gravada) quando o cliente e salvo.
var FI = window.FI || {};
window.FI = FI;

FI.Beneficiarios = (function ($) {

    var lista = [];          // [{ Id, CPF, Nome }]  (Id = 0 para beneficiarios ainda nao gravados)
    var indiceEdicao = -1;   // indice do beneficiario em alteracao (-1 = incluindo)

    function mostrarErro(mensagem) {
        $('#beneficiarioErro').text(mensagem).show();
    }

    function limparErro() {
        $('#beneficiarioErro').hide().text('');
    }

    function limparFormulario() {
        $('#BenefCPF').val('');
        $('#BenefNome').val('');
        $('#btnIncluirBenef').text('Incluir');
        indiceEdicao = -1;
        limparErro();
    }

    function botao(texto, acao, indice) {
        return $('<button type="button" class="btn btn-primary btn-sm"></button>')
            .text(texto)
            .attr('data-acao', acao)
            .attr('data-indice', indice);
    }

    function renderizar() {
        var $corpo = $('#tabelaBeneficiarios tbody').empty();

        if (lista.length === 0) {
            $corpo.append($('<tr></tr>').append(
                $('<td colspan="3" class="text-muted"></td>').text('Nenhum beneficiário incluído.')));
            return;
        }

        $.each(lista, function (i, b) {
            $corpo.append($('<tr></tr>')
                .append($('<td></td>').text(b.CPF))
                .append($('<td></td>').text(b.Nome))
                .append($('<td></td>')
                    .append(botao('Alterar', 'alterar', i))
                    .append(' ')
                    .append(botao('Excluir', 'excluir', i))));
        });
    }

    function incluirOuSalvar() {
        var cpf = $.trim($('#BenefCPF').val());
        var nome = $.trim($('#BenefNome').val());

        limparErro();

        if (!cpf)
            return mostrarErro('Informe o CPF do beneficiário.');
        if (!FI.Cpf.validar(cpf))
            return mostrarErro('O CPF informado é inválido.');
        if (!nome)
            return mostrarErro('Informe o nome do beneficiário.');

        var digitos = FI.Cpf.somenteDigitos(cpf);
        for (var i = 0; i < lista.length; i++) {
            if (i !== indiceEdicao && FI.Cpf.somenteDigitos(lista[i].CPF) === digitos)
                return mostrarErro('Já existe um beneficiário com este CPF para este cliente.');
        }

        if (indiceEdicao >= 0) {
            lista[indiceEdicao].CPF = FI.Cpf.formatar(cpf);
            lista[indiceEdicao].Nome = nome;
        } else {
            lista.push({ Id: 0, CPF: FI.Cpf.formatar(cpf), Nome: nome });
        }

        limparFormulario();
        renderizar();
        $('#BenefCPF').focus();
    }

    function iniciarAlteracao(indice) {
        var b = lista[indice];
        if (!b)
            return;

        limparErro();
        indiceEdicao = indice;
        $('#BenefCPF').val(b.CPF);
        $('#BenefNome').val(b.Nome);
        $('#btnIncluirBenef').text('Salvar');
        $('#BenefNome').focus();
    }

    function excluir(indice) {
        lista.splice(indice, 1);
        limparFormulario();
        renderizar();
    }

    function init() {
        if (!document.getElementById('modalBeneficiarios'))
            return;

        $('#btnBeneficiarios').on('click', function () {
            limparFormulario();
            renderizar();
            $('#modalBeneficiarios').modal('show');
        });

        $('#modalBeneficiarios').on('shown.bs.modal', function () {
            $('#BenefCPF').focus();
        });

        $('#modalBeneficiarios').on('hidden.bs.modal', limparFormulario);

        $('#btnIncluirBenef').on('click', incluirOuSalvar);

        $('#BenefCPF, #BenefNome').on('keydown', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                incluirOuSalvar();
            }
        });

        $('#tabelaBeneficiarios').on('click', 'button[data-acao]', function () {
            var indice = parseInt($(this).attr('data-indice'), 10);
            if ($(this).attr('data-acao') === 'alterar')
                iniciarAlteracao(indice);
            else
                excluir(indice);
        });

        renderizar();
    }

    return {
        init: init,

        // Carrega os beneficiarios ja gravados (tela de alteracao)
        carregar: function (itens) {
            lista = $.map(itens || [], function (b) {
                return { Id: b.Id, CPF: FI.Cpf.formatar(b.CPF), Nome: b.Nome };
            });
            renderizar();
        },

        // Lista a ser enviada ao servidor junto com o cliente
        obter: function () {
            return $.map(lista, function (b) {
                return { Id: b.Id, CPF: b.CPF, Nome: b.Nome };
            });
        },

        limpar: function () {
            lista = [];
            limparFormulario();
            renderizar();
        }
    };
})(jQuery);

$(function () {
    FI.Beneficiarios.init();
});
