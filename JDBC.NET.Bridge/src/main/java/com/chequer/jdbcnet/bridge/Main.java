package com.chequer.jdbcnet.bridge;

import com.chequer.jdbcnet.bridge.service.DatabaseServiceImpl;
import com.chequer.jdbcnet.bridge.service.DriverServiceImpl;
import com.chequer.jdbcnet.bridge.service.ReaderServiceImpl;
import com.chequer.jdbcnet.bridge.service.StatementServiceImpl;
import io.grpc.Server;
import io.grpc.ServerBuilder;
import org.apache.commons.cli.*;

import java.io.IOException;
import java.util.concurrent.TimeUnit;

public class Main {
    private static Server server;

    private static void start(int port) throws IOException {
        server = ServerBuilder.forPort(port)
                .addService(new DriverServiceImpl())
                .addService(new DatabaseServiceImpl())
                .addService(new StatementServiceImpl())
                .addService(new ReaderServiceImpl())
                .build()
                .start();

        Runtime.getRuntime().addShutdownHook(new Thread() {
            @Override
            public void run() {
                try {
                    if (server != null) {
                        server.shutdown().awaitTermination(30, TimeUnit.SECONDS);
                    }
                } catch (InterruptedException e) {
                    e.printStackTrace(System.err);
                }
            }
        });
    }

    private static void blockUntilShutdown() throws InterruptedException {
        if (server != null) {
            server.awaitTermination();
        }
    }

    public static void main(String[] args) {
        var options = new Options();

        var output = new Option("p", "port", true, "Specifies the port to open.");
        output.setRequired(true);
        options.addOption(output);

        var parser = new DefaultParser();

        try {
            var cmd = parser.parse(options, args);
            var port = cmd.getOptionValue("port");

            start(Integer.parseInt(port));
            System.out.println("JDBC.NET.Bridge is running on port " + port + "...");

            blockUntilShutdown();
        } catch (ParseException e) {
            System.out.println(e.getMessage());
            System.exit(1);
        } catch (InterruptedException | IOException e) {
            e.printStackTrace();
        }
    }
}
