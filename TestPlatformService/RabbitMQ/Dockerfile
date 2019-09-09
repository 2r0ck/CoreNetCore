FROM rabbitmq:3.7.17-management

MAINTAINER yarulin@gloria-jeans.ru

ADD rabbitmq.config /etc/rabbitmq/

RUN chown rabbitmq:rabbitmq /etc/rabbitmq/rabbitmq.config
CMD ["rabbitmq-server"]